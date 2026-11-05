"""Repeatable bidirectional UDP impairment proxy. One proxy per peer pair.
Example: python scripts/udp-test-proxy.py --listen 127.0.0.1:30000 --left 127.0.0.1:7000 --right 127.0.0.1:7001 --latency-ms 50 --jitter-ms 15 --loss .02
Configure BOTH peers to address this same proxy for the other player.
"""
import argparse
import heapq
import random
import selectors
import socket
import time

def endpoint(value):
    address, port = value.rsplit(":", 1)
    socket.inet_aton(address)
    return address, int(port)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--listen", type=endpoint, required=True)
    parser.add_argument("--left", type=endpoint, required=True)
    parser.add_argument("--right", type=endpoint, required=True)
    parser.add_argument("--latency-ms", type=float, default=50)
    parser.add_argument("--jitter-ms", type=float, default=15)
    parser.add_argument("--loss", type=float, default=.02)
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--max-pending", type=int, default=10000)
    args = parser.parse_args()
    if not 0 <= args.loss < 1 or args.latency_ms < 0 or args.jitter_ms < 0 or args.max_pending < 1:
        parser.error("Use nonnegative latency/jitter and loss in [0, 1).")
    rng = random.Random(args.seed)
    queue = []
    sequence = 0
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp, selectors.DefaultSelector() as selector:
        udp.bind(args.listen)
        udp.setblocking(False)
        selector.register(udp, selectors.EVENT_READ)
        try:
            while True:
                now = time.monotonic()
                while queue and queue[0][0] <= now:
                    _, _, data, target = heapq.heappop(queue)
                    try:
                        udp.sendto(data, target)
                    except (ConnectionResetError, ConnectionRefusedError):
                        pass  # A peer may have closed its socket during teardown.
                timeout = min(.01, max(0, queue[0][0] - now)) if queue else .01
                for _key, _events in selector.select(timeout):
                    try:
                        data, sender = udp.recvfrom(65535)
                    except (ConnectionResetError, ConnectionRefusedError, BlockingIOError):
                        continue
                    if sender not in (args.left, args.right) or rng.random() < args.loss or len(queue) >= args.max_pending:
                        continue
                    target = args.right if sender == args.left else args.left
                    delay = max(0, args.latency_ms + rng.uniform(-args.jitter_ms, args.jitter_ms)) / 1000
                    sequence += 1
                    heapq.heappush(queue, (time.monotonic() + delay, sequence, data, target))
        except KeyboardInterrupt:
            pass

if __name__ == "__main__":
    main()
