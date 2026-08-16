# Order state machine

Normalized leader states (adapter maps native NT values here):

```text
Observed → PendingSubmission → Working → PartiallyFilled → Filled
                                 ↓              ↓
                           CancelPending → Canceled
                                 ↓
                           ChangePending
                                 ↓
                           Rejected / UnknownTerminal
```

Logical copy states:

```text
Discovered → Validated → Dispatching → Active → PartiallySatisfied → Satisfied
                 ↓            ↓           ↓
               Failed      Divergent    Canceling → Canceled
                                              ↓
                                          Terminal
```

Transition function (implemented by `CopyCoordinator`):

```text
(previousState, normalizedEvent, activePolicy) -> intents + new state
```

## Order Mirror rules (V1)

| Leader observation | Engine/group allows new entries | Existing mapping | Intents |
| --- | --- | --- | --- |
| First working/pending/filled market, limit, stop, MIT | yes | no | `SubmitFollowerOrder` per enabled ready follower |
| Same semantic fingerprint | any | any | `NoOp` (`duplicate-fingerprint`) |
| Copier-originated (`TC:` / origin registry) | any | any | `NoOp` (`loop-prevention`) |
| Engine disabled or paused | no | no | no submit |
| Leader cancel / cancel-pending | risk-reducing allowed | yes | `CancelFollowerOrder` |
| Leader price change | any | yes | `ChangeFollowerOrder` |
| Leader quantity decrease | risk-reducing allowed | yes | change/cancel; never reverse |
| Unsupported type | any | no | `RaiseDivergence` |
| Enabled follower not ready | default policy | no | block group entries + divergence |

Execution Mirror Mode is rejected by `ConfigValidator` until Order Mirror is stable.
