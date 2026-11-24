# Codex usage report

Generated 2026-11-22 20:49 (Asia/Seoul) by codex-usage-report from the Codex session logs on this machine. Costs are what the same tokens would have cost at OpenAI API rates.

## Last 7 days

2026-11-16 to 2026-11-22

| Model | Requests | Input | Cached input | Output | Reasoning | Cached share | Cost |
|---|--:|--:|--:|--:|--:|--:|--:|
| gpt-6.2-astra | 3,498 | 622,225,716 | 614,266,880 | 1,318,228 | 261,553 | 98.7% | $362.11 |
| gpt-6.1-sol | 1,102 | 287,831,257 | 283,328,128 | 1,194,255 | 669,681 | 98.4% | $76.68 |
| gpt-oss:20b | 99 | 7,035,909 | 6,392,976 | 64,984 | 0 | 90.9% | N/A |
| **Total** | **4,699** | **917,092,882** | **903,987,984** | **2,577,467** | **931,234** | **98.6%** | **$438.79** |

Compaction made 36 of these requests, with 10,018,689 input tokens and $8.71 of the cost.

## Month to date

2026-11-01 to 2026-11-22

| Model | Requests | Input | Cached input | Output | Reasoning | Cached share | Cost |
|---|--:|--:|--:|--:|--:|--:|--:|
| gpt-6.2-astra | 3,498 | 622,225,716 | 614,266,880 | 1,318,228 | 261,553 | 98.7% | $362.11 |
| gpt-6.1-sol | 2,666 | 653,441,947 | 644,258,304 | 2,638,935 | 1,495,320 | 98.6% | $165.09 |
| gpt-6-astra | 128 | 19,700,570 | 19,431,168 | 72,891 | 21,814 | 98.6% | $25.77 |
| gpt-oss:20b | 242 | 17,689,554 | 16,768,938 | 130,293 | 0 | 94.8% | N/A |
| qwen3.8:27b-mlx | 28 | 2,657,073 | 2,163,819 | 44,766 | 0 | 81.4% | N/A |
| **Total** | **6,562** | **1,315,714,860** | **1,296,889,109** | **4,205,113** | **1,778,687** | **98.6%** | **$552.97** |

Compaction made 49 of these requests, with 13,806,663 input tokens and $11.11 of the cost.

## All time

2026-11-12 to 2026-11-22

| Model | Requests | Input | Cached input | Output | Reasoning | Cached share | Cost |
|---|--:|--:|--:|--:|--:|--:|--:|
| gpt-6.2-astra | 3,498 | 622,225,716 | 614,266,880 | 1,318,228 | 261,553 | 98.7% | $362.11 |
| gpt-6.1-sol | 2,666 | 653,441,947 | 644,258,304 | 2,638,935 | 1,495,320 | 98.6% | $165.09 |
| gpt-6-astra | 128 | 19,700,570 | 19,431,168 | 72,891 | 21,814 | 98.6% | $25.77 |
| gpt-oss:20b | 242 | 17,689,554 | 16,768,938 | 130,293 | 0 | 94.8% | N/A |
| qwen3.8:27b-mlx | 28 | 2,657,073 | 2,163,819 | 44,766 | 0 | 81.4% | N/A |
| **Total** | **6,562** | **1,315,714,860** | **1,296,889,109** | **4,205,113** | **1,778,687** | **98.6%** | **$552.97** |

Compaction made 49 of these requests, with 13,806,663 input tokens and $11.11 of the cost.

## Rate limits

The latest snapshot Codex logged for each bucket, not a total for any period. Times are in Asia/Seoul.

| Bucket | Plan | Window | Used | Resets | As of |
|---|---|---|--:|---|---|
| codex | self\_serve\_business\_prolite | weekly | 1% | 2026-11-29 20:46 | 2026-11-22 20:49 |

## Prices

Prices come from the LiteLLM public price table, fetched 2026-11-22 20:49.

## Checks

- 22 files read, 6,562 requests counted, 0 duplicates skipped.
- All 19 files with usage reconcile, meaning the per-request usage adds up to Codex's own running total.
- 0 directories and 0 files could not be read. 0 malformed lines in the middle of a file, 0 at the end of one.
- 0 requests with no model, 0 with no readable timestamp, 0 with cache-write tokens.
- Usage fields missing: none.
- Models: gpt-6-astra, gpt-6.1-sol, gpt-6.2-astra, gpt-oss:20b, qwen3.8:27b-mlx.
- Codex versions: 0.167.2, 0.169.0. Clients: codex-tui, codex\_exec. Session sources: exec, vscode.
- Reasoning efforts: high, medium, xhigh. Service tiers: default, priority.

## Warnings

- The logs show service tier "priority". This report prices every request at standard rates. Fast mode costs 2x, so the real cost can be about double this estimate.
- No published price for gpt-oss:20b and qwen3.8:27b-mlx, so they show N/A and are left out of the cost totals.

## Accuracy

- This is not a bill. Codex is a subscription, and these are the costs of the same tokens through the API.
- Fast mode is not visible in the logs ([openai/codex#30413](https://github.com/openai/codex/issues/30413)). Requests made in Fast mode cost 2x these rates.
- Cached input is priced at the cached rate, which assumes the API would have cached the same requests. OpenAI documents no difference in caching between subscription and API use.
- Cached input is part of input, and reasoning is part of output.

## Privacy

This report holds token counts, costs, model names, rate-limit percentages and Codex version strings. It holds no conversation text, no file paths, no thread names and no agent nicknames. For what the tool sends over the network, see [What this sends over the network](https://github.com/SergiPantoja/codex-usage#what-this-sends-over-the-network) in the README.
