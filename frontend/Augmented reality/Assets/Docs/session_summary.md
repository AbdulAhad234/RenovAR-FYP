# RenovAR Caching & Homepage Implementation Summary

This file serves as a reference summary of the changes made during this development session.

## 1. Fast Startup Caching System (Implemented)
- **Problem**: The app fetched data from the server on every launch, causing slow startup speeds. It also deleted the disk cache after 24 hours, meaning offline support would break if not opened daily.
- **Solution**: Implemented a **Stale-While-Revalidate** caching pattern:
  - The cache is loaded immediately at startup from `products_cache.json` (instant load).
  - A background thread (`FetchFromServerInBackground`) fetches updated products and refreshes the cache without blocking the UI.
  - A C# event (`OnProductsUpdated`) in `APIManager` notifies subscribers (like the UI spawner) when fresh data is ready.
  - Expired cache files are preserved as an offline fallback rather than deleted.
  - Added JSON caching for AR product data (`ar_product_{productId}.json`) so previously viewed/downloaded models can be placed in AR even when offline.

## 2. Homepage and Search System (Implemented)
- **Goal**: Implement a Homepage layout with:
  1. A horizontal auto-scrolling slideshow of 10 randomized products.
  2. A horizontal layout of 10 different products (Hot Topics).
  3. A search bar at the top filtering by name, description, brand, or vendor.
- **Data Mapping**: Added `brandName` and `vendorName` fields to `ProductData` in `ProductData.cs`.
- **UI Controller**: Created `HomepageManager.cs` to control UI bindings:
  - **Horizontal Auto-scroll**: Smoothly scrolls index-by-index using a Lerp-based coroutine. If the user touches or drags the screen (`Input.GetMouseButton(0)`), auto-scrolling is paused to respect manual navigation.
  - **Randomization**: Generates 10 random featured items and 10 different random hot topic items at launch.
  - **Filtering/Search**: Listens to changes in the search input field and filters products by matching query terms against names, descriptions, brandNames, vendorNames, and categoryNames.
  - **Background Refresh Integration**: Integrates with `APIManager.Instance.OnProductsUpdated` to automatically reload elements when background validation completes.

## 3. Homepage Scene Transitions & Details View AR Integration (Implemented)
- **Routing**: Updated `ProductItemUI.cs` to load either `ProductModelScene` or `ProductTextureScene` depending on the product type if clicked on the Homepage, setting a `cameFromHomepage` flag.
- **Seamless Loading**: Configured `AppUIManager.cs` to immediately present the product details view on scene load if a product was selected from the Homepage.
- **Back Button Routing**: Configured the Details panel back button to return to `"HomepageScene"` if the user came from the Homepage, resetting state.
- **Unified AR Mode**: Refactored the AR glTF downloader and model instantiator into `AppUIManager.cs` as a reusable helper `StartViewInARProcess`. Handled "View in AR" click events from the Details panel using this helper.

## 4. Smart Splash Scene Routing (Implemented)
- **Routing Decision**: Refactored `SplashSceneManager.cs` to decide which scene to load after pre-fetching completes based on user authentication status:
  - If the user is logged in $\rightarrow$ loads `mainSceneName` (configurable, default: `"HomepageScene"`).
  - If the user is NOT logged in $\rightarrow$ loads `loginSceneName` (configurable, default: `"LoginScene"`).
- **Scene Lists**: Added `List<string> appScenes` to expose all available app scenes in the Inspector for quick configuration.

## 5. Navigation System & Cart Button (Implemented)
- **NavigationManager.cs**: Global singleton (auto-creates via `[RuntimeInitializeOnLoadMethod]`) that tracks scene history in a `Stack<string>`. Also supports a local `Stack<Action>` for in-scene panel close actions, so `GoBack()` will close the top panel first, then fall back to loading the previous scene.
- **BackButton.cs**: Drop-on script for any back button. Has an Inspector dropdown (`BackActionType`) with three modes:
  1. `GlobalBack` — asks NavigationManager to close top panel or go back a scene.
  2. `CloseSpecificPanel` — directly `SetActive(false)` on a drag-and-dropped panel reference.
  3. `LoadSpecificScene` — force-loads a specified scene name.
- **GoToCartButton.cs**: Drop-on script for the Cart button prefab. Navigates to `CartScene` on click. Pair with existing `CartBadgeUI.cs` to show item count badge.

## 6. AR Bounding Box & Dimension Visualizer (Implemented)
- **BoundingBoxVisualizer.cs**: Attached automatically by `ARStandaloneManipulator` when an object is placed. Provides:
  - **12-edge wireframe cube** around the object's world-space bounding box using `LineRenderer`.
  - **Dual 3D TextMeshPro Labels** (billboarded, always facing camera):
    - **Width Label**: Placed on top of the bounding box showing width (e.g. `120 cm W`). Shows rotation degrees during rotation.
    - **Height Label**: Positioned on the right side of the bounding box relative to the camera's view direction, centered vertically (e.g. `75 cm H`). Deactivated during rotation.
  - **Dynamic Perspective Following**: Recalculates label positions in `LateUpdate` so that as the camera orbits, the width label stays on top and the height label dynamically orbits to remain on the screen's right side.
  - **During Scale**: Line and text colors change to orange (too small), red (too big), or green (optimal).
  - **Optimal Snap Feedback**: When dimensions hit the optimal size (from `ProductData.dimensions`), lines and text flash green for 1 second then return to white. Scale **locks** at optimal until the user releases and pinches again.
  - **During Rotate**: Width label shows rotation angle in degrees (0°–360°).
  - Stays visible while object is selected. Hidden on deselect.
  - **Custom Font Support**: Added support for Poppins-Light SDF font asset (`Assets/AssetData/Poppins/Poppins-Light SDF.asset`). You can assign it via the `poppinsFont` field on the `SurfacePlacementManager` component in the Inspector.
- **ARStandaloneManipulator.cs**: Updated to create `BoundingBoxVisualizer`, set optimal dimensions from `ProductData` (API-first, then name-based estimation for 40+ product types), and fire `OnScale*` / `OnRotate*` callbacks during 2-finger pinch/twist gestures.
- **Performance Optimizations**: Cached renderers, shared line material, dirty-flag text/color updates, bounds-change thresholds, reusable hit lists, cached `ARPlaneManager`.

## 7. AR Object Pooling System (Implemented)
- **PlacedObjectsManager.cs**: Rewritten as an object pool:
  - **Delete All** (`PoolAllObjects`) sets objects to `SetActive(false)` instead of destroying them. They persist in the pool for the session.
  - **Same Product Reuse**: If the user views the same product again in AR, the pooled object is reactivated at the new tap position — no re-download or re-instantiation needed.
  - **Different Product**: When a new product is selected, old pooled objects are destroyed (`EvictPoolExcept`) to free memory.
  - **App Quit**: All objects (active + pooled) are fully destroyed via `OnApplicationQuit`.
  - Pool is keyed by product ID, extracted from the naming convention `Cached_AR_Product_{id}`.
  - Added `[RuntimeInitializeOnLoadMethod]` to automatically instantiate the `PlacedObjectsManager` singleton on startup. This fixes the issue where `PlacedObjectsManager.Instance` was null, which previously bypassed pooling and destroyed the objects, causing re-placement taps to fail.
  - Fixed `OnSceneLoaded` scene name check to recognize `ProductModelScene` and `ProductTextureScene` as AR scenes instead of a hardcoded `"ARScene"`.
- **AppUIManager.cs**: `ClearAllPlacedObjects` now pools instead of destroying. `StartViewInARProcess` checks the pool first before downloading.
- **SurfacePlacementManager.cs**: `PerformPlacement` passes product ID to `RegisterPlacedObject`, and skips `NormalizeModelScale` for pooled objects (they retain their last scale).

## 8. AR Texture Visualization for Floors & Walls (Implemented)
- **Problem**: Flooring and wall paneling products had their "View in AR" buttons disabled since they are materials/textures, not 3D models.
- **Solution**: Enabled AR preview for textures by applying downloaded textures dynamically to detected floor (horizontal) or wall (vertical) planes:
  - **ProductData.cs**: Added `IsFloorTexture` and `IsWallTexture` helper properties mapping product categories (e.g., Flooring, Wall Paneling) and keywords to their respective plane alignment targets.
  - **SelectedProductHolder.cs**: Added fields for storing and retrieving the active texture and tracking the current AR mode.
  - **ProductDetailsUI.cs & ProductItemUI.cs**: Enabled the AR preview button for textures, updating the label to "Preview in AR" and routing to `StartTextureARProcess` on click.
  - **AppUIManager.cs**: Added `StartTextureARProcess` which downloads the texture using cached imagery or `UnityWebRequestTexture`, sets up the state, and transitions to AR mode. It also initializes `SurfacePlacementManager` in texture mode so that placement visualizers, target filters, and inputs run correctly.
  - **TexturePlacementManager.cs**: Core component attached to XR Origin that:
    - Supports manual texture placement via a new `autoApplyToAllPlanes` option (default: false) that expects user touch/click interaction rather than auto-applying.
    - Listens to `ARPlaneManager.planesChanged` to track new/updated planes, and dynamically re-applies/updates textures if AR Foundation resets them during plane shape updates.
    - Configures texture properties (trilinear filtering, anisotropy) for sharp, realistic oblique rendering.
    - Creates a shared material using a custom shader (`Custom/WorldSpaceTexture`) to project textures seamlessly based on world space coordinates, eliminating boundaries, misalignment, and stretching where adjacent planes meet. Falls back to URP Lit, Standard, or Unlit if needed.
    - Sets UV tiling scale to a fixed 1x1 (Vector2.one) for fallback shaders.
    - Caches original materials to allow restoration via a "Clear" button (`ClearTexturedPlanes`).
    - Provides floor/wall target toggles.
  - **SurfacePlacementManager.cs**: Integrated manual texture placement. In texture mode, the placement visualizer remains active (displaying the placement indicator on valid detected surfaces matching horizontal/vertical rules) and routes taps or Place button clicks to apply the texture to the specific plane hit by the raycast.

## 9. AR Lifecycle Subsystem Fix (Implemented)
- **Problem**: Changing scenes from `ProductModelScene` (AR) $\rightarrow$ `HomepageScene` (Non-AR) $\rightarrow$ `ProductTextureScene` (AR) resulted in a `MissingReferenceException` inside Unity's `SimulationSessionSubsystem`. The AR subsystem attempted to access properties (like `cullingMask`) of the main camera from the previous scene which had already been destroyed during the transition.
- **Solution**: Implemented a coordinated **Pre-Initialization & Post-Deinitialization** system:
  - **Pre-Initialization**: Before starting to load any AR scene (in `ProductItemUI.cs`, `HomepageManager.cs`, `CartUIController.cs`, and `NavigationManager.GoBack()`), `LoaderUtility.Initialize()` is invoked. This ensures the AR Loader (including Editor Simulation subsystems) is fully active *before* the scene objects start loading and their `ARSession` component executes `OnEnable()`. This resolves the black camera issue and ensures the editor simulation environment is generated successfully.
  - **Post-Deinitialization**: In `NavigationManager.OnSceneLoaded()`, if the newly loaded scene is a **non-AR** scene (e.g., Homepage), it invokes `LoaderUtility.Deinitialize()`. This cleanly shuts down all AR subsystems and releases destroyed camera references.
  - This solves the scene transition lifecycle bug across the entire application in both Editor Simulation and physical device environments.
