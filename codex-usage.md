# Codex usage report

Generated 2026-11-22 13:17 (Asia/Seoul) by codex-usage-report from the Codex session logs on this machine. Costs are what the same tokens would have cost at OpenAI API rates.

## Last 365 days

2025-11-23 to 2026-11-22

| Model | Requests | Input | Cached input | Output | Reasoning | Cached share | Cost |
|---|--:|--:|--:|--:|--:|--:|--:|
| gpt-6.2-astra | 2,344 | 412,667,526 | 406,964,864 | 882,093 | 171,527 | 98.6% | $243.69 |
| gpt-6.1-sol | 2,666 | 653,441,947 | 644,258,304 | 2,638,935 | 1,495,320 | 98.6% | $165.09 |
| gpt-6-astra | 128 | 19,700,570 | 19,431,168 | 72,891 | 21,814 | 98.6% | $25.77 |
| gpt-oss:20b | 242 | 17,689,554 | 16,768,938 | 130,293 | 0 | 94.8% | N/A |
| qwen3.8:27b-mlx | 28 | 2,657,073 | 2,163,819 | 44,766 | 0 | 81.4% | N/A |
| **Total** | **5,408** | **1,106,156,670** | **1,089,587,093** | **3,768,978** | **1,688,661** | **98.5%** | **$434.55** |

Compaction made 40 of these requests, with 11,605,932 input tokens and $8.63 of the cost.

## Month to date

2026-11-01 to 2026-11-22

| Model | Requests | Input | Cached input | Output | Reasoning | Cached share | Cost |
|---|--:|--:|--:|--:|--:|--:|--:|
| gpt-6.2-astra | 2,344 | 412,667,526 | 406,964,864 | 882,093 | 171,527 | 98.6% | $243.69 |
| gpt-6.1-sol | 2,666 | 653,441,947 | 644,258,304 | 2,638,935 | 1,495,320 | 98.6% | $165.09 |
| gpt-6-astra | 128 | 19,700,570 | 19,431,168 | 72,891 | 21,814 | 98.6% | $25.77 |
| gpt-oss:20b | 242 | 17,689,554 | 16,768,938 | 130,293 | 0 | 94.8% | N/A |
| qwen3.8:27b-mlx | 28 | 2,657,073 | 2,163,819 | 44,766 | 0 | 81.4% | N/A |
| **Total** | **5,408** | **1,106,156,670** | **1,089,587,093** | **3,768,978** | **1,688,661** | **98.5%** | **$434.55** |

Compaction made 40 of these requests, with 11,605,932 input tokens and $8.63 of the cost.

## All time

2026-11-12 to 2026-11-22

| Model | Requests | Input | Cached input | Output | Reasoning | Cached share | Cost |
|---|--:|--:|--:|--:|--:|--:|--:|
| gpt-6.2-astra | 2,344 | 412,667,526 | 406,964,864 | 882,093 | 171,527 | 98.6% | $243.69 |
| gpt-6.1-sol | 2,666 | 653,441,947 | 644,258,304 | 2,638,935 | 1,495,320 | 98.6% | $165.09 |
| gpt-6-astra | 128 | 19,700,570 | 19,431,168 | 72,891 | 21,814 | 98.6% | $25.77 |
| gpt-oss:20b | 242 | 17,689,554 | 16,768,938 | 130,293 | 0 | 94.8% | N/A |
| qwen3.8:27b-mlx | 28 | 2,657,073 | 2,163,819 | 44,766 | 0 | 81.4% | N/A |
| **Total** | **5,408** | **1,106,156,670** | **1,089,587,093** | **3,768,978** | **1,688,661** | **98.5%** | **$434.55** |

Compaction made 40 of these requests, with 11,605,932 input tokens and $8.63 of the cost.

## Rate limits

The latest snapshot Codex logged for each bucket, not a total for any period. Times are in Asia/Seoul.

| Bucket | Plan | Window | Used | Resets | As of |
|---|---|---|--:|---|---|
| codex | self\_serve\_business\_prolite | weekly | 50% | 2026-11-23 19:03 | 2026-11-22 13:16 |

## Prices

Prices come from the LiteLLM public price table, fetched 2026-11-22 13:17.

## Checks

- 22 files read, 5,408 requests counted, 0 duplicates skipped.
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
