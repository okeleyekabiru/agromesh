# Crop management (database-backed crops)

## Goal
Replace the closed `CropType` enum (OilPalm, Maize, Mixed, Cocoa, Cassava) with a `Crops` table so a farm can add crops (for example Yam) that blocks link to. Built-ins stay as a domain Smart Enum (`BuiltInCrop`) carrying display name and fertilizer profile; Plantain and Banana are new built-ins. Existing data migrates losslessly.

## Design decisions
- `Crop` entity / `Crops` table: `Id`, `FarmId` (null for built-ins), `Name`, `IsBuiltIn`. Built-ins are seeded by migration `AddCrops` with `Id` = the old enum member name (`OilPalm`, `Maize`, `Cocoa`, `Cassava`, `Plantain`, `Banana`). Custom crops get a GUID id, are per farm, name only, and use the generic fertilizer cycle (every 12 weeks).
- `BuiltInCrop` smart enum (`Ilevora.Domain.Crops`): Id, Name, legacy ordinal, fertilizer plan. `Mixed` is kept as a non-selectable legacy member so old data reads back; `Block.ValidateCrops` still rejects it.
- `BlockCrop` (JSON on `Block.Crops`) is now `(CropId, CropName, Hectares, Percentage)`. `CropName` is denormalised so reports and calculators need no lookup (custom crops cannot be renamed). A custom JSON converter reads old rows (`{"CropType":0,...}`, number or name) as the same crop, so **no data rewrite is needed**; rows are rewritten in the new shape on the next save.
- Crop figure keys (CropPnl, `Transaction.CropAllocation`, report `cropType`) stay `OilPalm`, `Maize`, ... for built-ins (identical to the old `ToString()`), and the crop name for custom crops. Existing stored allocations keep matching.
- **Wire compatibility**: responses carry `cropType` (old enum ordinal for the original five; `2` for Plantain, Banana and custom crops, as old clients have no other slot) plus `cropId` and `cropName`. Requests accept `cropId` (preferred) or the legacy `cropType` ordinal. Sending Mixed (ordinal 2 / id `Mixed`) is still rejected.
- Endpoints: `GET /api/farms/{farmId}/crops` (built-ins then the farm's crops), `POST /api/farms/{farmId}/crops` body `{name}` (400 on blank, duplicate of a built-in or an existing farm crop, case-insensitive; 404 for another company's farm).

## Requirements and acceptance criteria (all covered by tests)
- [x] Old stored JSON ordinals 0 to 4 read back as the same crop (`CropManagementTests.Stored_blocks_with_the_old_enum_ordinal...`).
- [x] Crops round-trip through JSON (`Crops_round_trip_through_json`).
- [x] Built-ins keep legacy ordinals; Plantain and Banana exist; Mixed is not selectable (`Built_in_crops_keep_old_ordinals...`).
- [x] Fertilizer profiles unchanged for the originals, plus Plantain and Banana (`FertilizerCycleTests.Each_crop_has_its_own_cycle`); custom crops use the generic cycle (`A_custom_crop_gets_the_generic_fertilizer_cycle`).
- [x] Mixed is rejected on a block (`Mixed_is_still_not_a_real_crop`, `FertilizerCycleTests`).
- [x] Old clients' ordinal input resolves; crop ids resolve for built-in and own custom crops; another farm's or unknown ids are rejected (`CropResolver` tests).
- [x] Create / list crops, validation, duplicates, company scoping (`CropManagementTests`).
- [x] Dashboard CropPnl, narration allocation, reports unchanged for built-ins (existing `CropPnlFallbackTests`, `DashboardCropPnlTests`, `NarrationInterpreterTests`, `ReportBuilderTests`).
- [x] Web: `cropLabel` and block normalisation (`web/src/utils/cropLabel.test.ts`); type check and build green.
- [x] Mobile: analyze clean of new issues; model tests (see report).

## Non-goals
Renaming or deleting custom crops; per-crop fertilizer editing; a crops management screen; re-keying old `CropAllocation` data; Playwright e2e (suite is red for unrelated reasons).

## Migration
`AddCrops` (create table, seed six rows). Live DB was not running during the build (no `ilevora_db` container), so no backup/apply was done; the API applies it on startup. Back up first per CLAUDE.md.
