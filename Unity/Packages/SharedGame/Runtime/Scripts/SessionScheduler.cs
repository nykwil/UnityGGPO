using System;
namespace SharedGame {
    /// <summary>Nonblocking 60 Hz pacing with a bounded backlog and at most four ticks per presentation update.</summary>
    public sealed class SessionScheduler {
        const double Interval = 1.0 / 60;
        double next;
        public void Reset(double now) { next = now; }
        public int TicksDue(double now, float framesAhead = 0) {
            if (double.IsNaN(now) || double.IsInfinity(now)) throw new ArgumentException("Invalid clock.");
            double step = Interval * (framesAhead > 2 ? 1.05 : 1);
            int ticks = 0;
            while (now >= next && ticks < 4) { ticks++; next += step; }
            if (now - next > Interval * 4) next = now + Interval;
            return ticks;
        }
    }
}
