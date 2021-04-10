# Comparison to Locust

# Setup

Both applications running against a NGINX Hello World Demo. 
Both running with 30 threads and wait time of 1 second between requests.

Unfortunately I can't get the warmup/spawnrate to work correctly with locust,
but that's another one of my pain points with it in general.

# Results

- PerformanceTest: Consistently 7500 RPS, Max Response Time 1100ms, Min: 0ms, 99th: 80ms
- Locust: Inconsistent, max about 3000 RPS, Max Response Time 22000ms, Min: 0ms, 99th: 17000ms

# Usage

- PerformanceTest: Mostly hanging on synchronization. Average CPU usage was around 30%.
- Locust: Maxing out CPU, usage at about 80%.