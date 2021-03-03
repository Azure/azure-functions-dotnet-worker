# AZFW0001: Invalid binding attributes

| | Value |
|-|-|
| **Rule ID** |AZFW0001|
| **Category** |[Usage]|
| **Severity** |Error|


## Cause

This rule is triggered when invalid, WebJobs, binding attributes are used in the function definition.

## Rule description

The Azure Functions .NET Worker uses a different input and output binding model, which is incompatible with the WebJobs binding
model used by the Azure Functions in-process model.

In order to support the existing bindings and triggers, a new set of packages, compatible with the new binding model, have been introduced, those
packages follow a naming convention that makes it easy to find a suitable replacement, simply by changing the prefix `Microsoft.Azure.WebJobs.Extensions.*` for `Microsoft.Azure.Functions.Worker.Extensions.*`. For example:

If you have a reference to `Microsoft.Azure.WebJobs.Extensions.ServiceBus`, replace that with a reference to `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`

## How to fix violations

To fix violations, add a reference to the appropriate package as described above and use the correct attributes from that package.

## When to suppress the rule

This rule should not be suppressed, as the existing bindings will not work in the isolated model.
