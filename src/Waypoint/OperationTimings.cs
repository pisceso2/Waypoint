using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace Waypoint;

/// <summary>One operation's numbers over the shown window, a row of the timings tab. <see cref="P95"/> and
/// <see cref="P99"/> are the worst second's: the runtime publishes quantiles per interval, not over the window.</summary>
sealed record OperationRow((string Phase, string Operation) Key, string Kind, string Label, double PerSecond, double Average,
    double P95, double P99, double MsPerSecond)
{
    public string KindText => L.Text(Kind);
}

/// <summary>
/// What each Gateway operation costs, from the server's <c>LocalExile.Gateway</c> meter: its
/// <c>localexile.gateway.duration</c> histogram (ms, tags <c>phase</c> and <c>operation</c>) as the runtime's
/// <c>System.Diagnostics.Metrics</c> event source publishes it once a second (<see cref="RuntimeCounters"/>): per
/// operation the interval's count, sum and p50/p95/p99. Kept from the launcher's start, across Gateway restarts.
/// </summary>
sealed class OperationTimings
{
    public const string Meter = "LocalExile.Gateway", Instrument = "localexile.gateway.duration";
    public const string Messages = "Client messages", Phases = "Server phases", LevelGen = "Level generation";

    readonly record struct Interval(double T, double Count, double Sum, double P95, double P99);

    sealed class Operation
    {
        public readonly string Kind, Label;
        public readonly Queue<Interval> Intervals = new();
        public readonly TimeSeries P95, Load;

        public Operation(string phase, string operation)
        {
            Kind = phase switch { "Message" => Messages, "LevelGen" => LevelGen, _ => Phases };
            // A message by its id, a generated area by its id, a phase by its name and, when it has one, its operation.
            Label = phase is "Message" or "LevelGen" ? operation : operation == "all" ? phase : $"{phase} ({operation})";
            // The tab colours the busiest ones by rank.
            P95 = new(Label, Colors.Gray);
            Load = new(Label, Colors.Gray);
        }
    }

    /// <summary>How far back the table can look: the longest window the tab offers.</summary>
    const double KeptSeconds = 3600;

    readonly Dictionary<(string, string), Operation> _operations = [];
    double _first = double.NaN;

    /// <summary>One interval of one operation, at <paramref name="t"/> seconds of the launcher's clock. An interval without
    /// calls is a gap in its p95 line and zero load.</summary>
    public void Add(double t, string phase, string operation, double count, double sum, double p95, double p99)
    {
        lock (_operations)
        {
            if (double.IsNaN(_first)) _first = t;
            if (!_operations.TryGetValue((phase, operation), out var op)) _operations[(phase, operation)] = op = new(phase, operation);
            if (count > 0) op.Intervals.Enqueue(new(t, count, sum, p95, p99));
            while (op.Intervals.TryPeek(out var old) && old.T < t - KeptSeconds) op.Intervals.Dequeue();
            op.P95.Add(t, count > 0 ? p95 : double.NaN);
            op.Load.Add(t, count > 0 ? sum : 0);
        }
    }

    /// <summary>Every operation called in the window (<paramref name="from"/> to <paramref name="now"/>), busiest first.
    /// Rates are over the part of the window the launcher has seen.</summary>
    public List<OperationRow> Window(double from, double now)
    {
        lock (_operations)
        {
            double seconds = Math.Max(1, now - Math.Max(from, double.IsNaN(_first) ? from : _first));
            var rows = new List<OperationRow>();
            foreach (var (key, op) in _operations)
            {
                double count = 0, sum = 0, p95 = 0, p99 = 0;
                foreach (var i in op.Intervals)
                {
                    if (i.T < from) continue;
                    count += i.Count;
                    sum += i.Sum;
                    p95 = Math.Max(p95, i.P95);
                    p99 = Math.Max(p99, i.P99);
                }
                if (count > 0) rows.Add(new(key, op.Kind, op.Label, count / seconds, sum / count, p95, p99, sum / seconds));
            }
            return [.. rows.OrderByDescending(r => r.MsPerSecond)];
        }
    }

    /// <summary>Drops every interval kept so far, of every operation: the tab starts again from the next published one and
    /// measures the window's rates from there. Only the Reset button does it; a Gateway restart keeps what was collected.</summary>
    public void Reset()
    {
        lock (_operations)
        {
            _operations.Clear();
            _first = double.NaN;
        }
    }

    /// <summary>The window's rows as CSV, the file a bug report attaches: an English header, one line per operation, the
    /// numbers in the invariant culture so the file reads the same in every interface language. <paramref name="kind"/>
    /// filters the rows as the tab's picker does; an empty one keeps every kind.</summary>
    public string Csv(double from, double now, string kind)
    {
        var text = new StringBuilder("operation,kind,calls_per_second,average_ms,p95_ms,p99_ms,ms_per_second\r\n");
        foreach (var row in Window(from, now))
        {
            if (kind.Length > 0 && row.Kind != kind) continue;
            text.Append(Field(row.Label)).Append(',')
                .Append(Field(row.Kind)).Append(',')
                .Append(row.PerSecond.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Average.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.P95.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.P99.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.MsPerSecond.ToString("0.##", CultureInfo.InvariantCulture)).Append("\r\n");
        }
        return text.ToString();
    }

    /// <summary>A CSV field, quoted when it holds a comma, a quote or a line break.</summary>
    static string Field(string value) => value.IndexOfAny([',', '"', '\r', '\n']) < 0 ? value : '"' + value.Replace("\"", "\"\"") + '"';

    /// <summary>An operation's charted series: its p95 per second and its milliseconds per second.</summary>
    public (TimeSeries P95, TimeSeries Load) Series((string Phase, string Operation) key)
    {
        lock (_operations) return _operations.TryGetValue(key, out var op) ? (op.P95, op.Load) : throw new KeyNotFoundException();
    }

    /// <summary>The <c>phase</c> and <c>operation</c> of a published tag list ("phase=Message,operation=0x0025").</summary>
    internal static (string Phase, string Operation)? Tags(string? tags)
    {
        string? phase = null, operation = null;
        foreach (var pair in (tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var (key, value) = (pair[..eq], pair[(eq + 1)..]);
            if (key == "phase") phase = value;
            else if (key == "operation") operation = value;
        }
        return phase is null ? null : (phase, operation ?? "all");
    }

    /// <summary>p95 and p99 of a published quantile list ("0.5=1.2;0.95=3.4;0.99=5.6"); null for an empty interval.</summary>
    internal static (double P95, double P99)? Quantiles(string? quantiles)
    {
        double p95 = double.NaN, p99 = double.NaN;
        foreach (var pair in (quantiles ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0 || !double.TryParse(pair[(eq + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) continue;
            switch (pair[..eq])
            {
                case "0.95": p95 = v; break;
                case "0.99": p99 = v; break;
            }
        }
        return double.IsNaN(p95) || double.IsNaN(p99) ? null : (p95, p99);
    }
}
