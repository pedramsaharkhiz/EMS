# ADR-0031: Persist the equipment conflict flag

- Status: Accepted
- Date: 2026-11-14

## Context
Equipment needs an editable conflict flag in CRUD and related equipment responses.

## Decision
Store `Equipment.HasConflict` as a required boolean with a SQL default of false. Carry it through existing shared read projections and DTOs; related response fields use `EquipmentHasConflict`. This is a current equipment value, not a historical snapshot. Existing location and status behavior stays independent.

Migration generation and application are left to the operator as requested, including the model snapshot. The schema drift check is expected to fail until then; no check is disabled.

## Alternatives
No alternative persistence design was evaluated; this follows the repository's existing entity and read model patterns.

## Consequences
Existing database rows receive false when the generated migration is applied. Deploy the migration before running the updated API against SQL Server.
