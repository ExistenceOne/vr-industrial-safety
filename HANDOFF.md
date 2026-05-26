# Vive 1.5 Controller → Unity XR Device Simulator Handoff

## Asset Overview

This folder contains the HTC Vive Controller 1.5 (SteamVR render model), exported as OBJ + MTL + textures.

| File | Purpose |
|---|---|
| `vr_controller_vive_1_5.obj` | Main body mesh |
| `vr_controller_vive_1_5.mtl` | Material definitions |
| `vr_controller_vive_1_5.json` | Component motion data (pivot points, axes, value mappings) |
| `onepointfive_texture.png` | Diffuse/Albedo (sRGB) |
| `onepointfive_spec.png` | Specular map (sRGB) |
| `onepointfive_occ_bake.tga` | Ambient occlusion bake (Linear) |
| `trigger.obj`, `trackpad.obj`, `l_grip.obj`, `r_grip.obj`, `sys_button.obj`, `button.obj`, etc. | Animated sub-components |

The `.mtl` defines two materials:
- `initialShadingGroup` — grey, no texture (LED and minor parts)
- `lambert4SG` — main controller body; uses `onepointfive_texture.png` (diffuse) + `onepointfive_spec.png` (specular)

---

## Import Checklist

### 1. Copy folder into Unity Assets/
Drop entire folder into `Assets/`. Unity auto-reads the `.mtl` and generates `.mat` files under `Materials/`.

### 2. Texture Import Settings

| Texture | sRGB | Notes |
|---|---|---|
| `onepointfive_texture.png` | ON | Base color |
| `onepointfive_spec.png` | ON | Specular color in gamma space |
| `onepointfive_occ_bake.tga` | OFF | Linear data — must be off or occlusion looks wrong |

### 3. Fix `lambert4SG.mat` (the auto-generated material)

- Shader: **URP > Lit** (or Built-in Standard)
- Workflow Mode: **Specular** (not Metallic — the original uses specular maps)
- Base Map → `onepointfive_texture.png`
- Specular Map → `onepointfive_spec.png`
- Occlusion Map → `onepointfive_occ_bake.tga`

### 4. Build Controller Prefab

1. Create empty GameObject: `ViveController`
2. Drag each imported mesh as a child at (0,0,0):
   - `vr_controller_vive_1_5` (body)
   - `trigger`, `trackpad`, `trackpad_touch`, `l_grip`, `r_grip`
   - `sys_button`, `button`, `led`, `scroll_wheel`
3. The `.json` local origins/rotations are already baked into each OBJ — no manual offset needed at import.

### 5. Assign to XR Device Simulator

- Open the XR Device Simulator prefab (from XRI package samples).
- On `LeftHand Controller` / `RightHand Controller` → **XR Controller** component → **Model Prefab** field.
- Assign `ViveController` prefab.

---

## Animation Reference (from vr_controller_vive_1_5.json)

| Part | Motion | Input Path | Range |
|---|---|---|---|
| `trigger` | Rotate X around pivot `[0, -0.016, 0.039]` | `/input/trigger` | 0° to -17° |
| `trackpad` | Trackpad XY + tilt | `/input/trackpad` | press_rotation ±7°/±4° |
| `l_grip` | Rotate Y around `[-0.019, -0.006, 0.075]` | `/input/grip/click` | 0° to 2° |
| `r_grip` | Rotate Y around `[0.019, -0.006, 0.075]` | `/input/grip/click` | 0° to -2° |
| `sys_button` | Translate Y | `/input/system/click` | 0 to -0.00075m |
| `button` | Translate Y | `/input/application_menu/click` | 0 to -0.00075m |
| `scroll_wheel` | Rotate X | `/input/trackpad/y` | 0° to -40° |

Drive transforms from XRI action values (`selectInteractionState.value` for trigger, `activateInteractionState` for grip/buttons).

---

## Known Gotchas

- **Specular vs Metallic workflow**: Do not use Metallic workflow — the asset has no metallic/roughness data. Use Specular workflow or smoothness will look wrong.
- **Occlusion TGA must be Linear**: Forgetting to uncheck sRGB on `onepointfive_occ_bake.tga` makes shadowed areas look muddy/incorrect.
- **`map_Ks` not auto-imported**: Unity's OBJ importer may ignore the specular map from the `.mtl`. Always check `lambert4SG.mat` after import and assign manually.
- **trackpad_touch visibility**: `trackpad_touch.obj` should be hidden by default; show only on touch/press input. Set `GameObject.SetActive()` from script.
- **scroll_wheel / trackpad_scroll_cut**: These replace the regular trackpad during scroll — toggle visibility via the `scroll` visibility flag in the JSON.

---

## Next Steps (not yet done)

- [ ] Assign animator or script to drive sub-component transforms from XRI input actions
- [ ] Test in Play mode with XR Device Simulator — confirm trigger/grip/button motion
- [ ] If using HDRP: bake ORM mask map (R=occlusion, A=smoothness from inverted spec grayscale)
- [ ] Optional: set up left/right variants using `openxr_handmodel` / `openxr_handmodel_r` component origins from JSON
