# Poly Parcel - code map

Everything is built at runtime from primitives. `PrototypeBootstrap` goes on ONE empty GameObject and creates the rest.
Big classes are `partial`, split by concern into a folder named after the class (the main file keeps fields, setup and per-frame logic).

## Where to change what
| I want to change... | Look in |
|---|---|
| Any tunable number (speeds, odds, points, throw feel, bounce) | `GameConfig.cs` (all values), `Scoring.cs` (points) |
| Dev menu sliders | `DevMenu.cs` |
| Rules, lives, score, run flow | `GameManager.cs`, `GameTime.cs`, `Progress.cs` (saved data) |
| Throwing and flight | `PaperThrower.cs`, `Paper.cs`, `BankSurface.cs`, `StickySurface.cs`, `TrampolineSurface.cs`, `ChuteSurface.cs` (porch-roof trick shot) |
| Bike and rider | `BikeForward.cs`, `Rider.cs`, `BikeShadow.cs`, `CameraFollow.cs` |
| Road, footpaths, lane paint, crossing | `RoadDetail.cs`, `SegmentSpawner/` |
| Rain and wet surfaces | `Wetness.cs`, `DayCycle.cs` |
| Sky, sun, clouds, fog, mist | `Backdrop.cs`, `DayCycle.cs`, `MistBank.cs`, `Look.cs` |
| Night lighting | `NightLights.cs`, `NightLamps.cs`, `House/House.Lighting.cs` |
| Menus and HUD | `GameUI.cs`, `GameUI/`, `UiTheme.cs`, `OptionsMenu.cs` |
| Tutorial | `Tutorial.cs`, `Tutorial/`, `SegmentSpawner/SegmentSpawner.Tutorial.cs` |

## House (one lot, pooled and re-laid-out by `Setup()`)
| File | Holds |
|---|---|
| `House.cs` | types, fields, `Setup`, `Update`, ground-floor scoring (`HandleHit`), status colours |
| `House/House.Yard.cs` | paved yards, garden table, trees and porches, trampoline, wall bushes |
| `House/House.Hoop.cs` | wall-mounted hoop and ring scoring |
| `House/House.Reverse.cs` | reverse basket: a trampoline with a ring above it (wall or stand), scored from below in Paper |
| `House/House.WallPad.cs` | the wall trampoline: an upright bouncy pad against the wall of some customers' houses (Paper bounces off it via `TrampolineSurface.vertical`) |
| `LowPolyTree.cs` | one low-poly tree: faceted crown mesh built at runtime (round, wide, tall or cone shapes in varied colours), flared forked trunk, roots, sway; solid trunk collider and a leaf trigger that holds a paper (`TreeSway`, `TreeFoliage`); every tree reaches `TreeMinTop`..`TreeMaxTop` (second floor); also hands out a cheap shared copy (coarse crown, 5-sided trunk, shared materials, no collider) for `BackRow`'s background trees |
| `LowPolyCar.cs` | low-poly vehicles (sedan, SUV, police car, box truck): one shared mesh per type and colour, box colliders with `BankSurface` |
| `House/House.Tree.cs` | places `LowPolyTree`s on a lot: the lawn tree (off the path and doormat) and the two trees that block throws (`LayoutLawnTree`, `PlaceBlockerTree`), and the dense park trees (`LayoutParkTrees`: trunks never closer than `ParkTreeGap`, crowns may overlap) |
| `House/House.Cars.cs` | places the pooled `LowPolyCar`s in yards and car parks (`PlaceCar`) |
| `House/House.Facade.cs` | cosmetic facade: pilasters, cornice, drainpipe, wall lamps, window frames (plain, pediment, shutters, tin awning, open slatted shutters), door surround, balcony awnings, hipped-roof dormers and chimney pipes, flat-roof parapets/terraces (picket fence, awning, table, crates, plants) and rooftop props, bench and hydrant, shop/hotel signs and blade signs. One collider-free mesh per house (`FacadeMesh.cs`), looks only |
| `House/House.Grass.cs` | low-poly grass tufts over a dark turf layer on lawns (clear of path, doormat, trees, table) and through park woods; one collider-free mesh per house (`FacadeBuilder.Tuft`) |
| `FacadeMesh.cs` | `FacadePalette` (one shared material, any colour via a 32x32x32 palette texture) and `FacadeBuilder` (boxes/prisms into one mesh) |
| `SignArt.cs` | text signs drawn in code with the pixel font of `BillboardArt` (name boards with an LED strip, blade signs); unlit, so they glow at night |
| `House/House.Floors.cs` | floors (1-3 storeys), the balcony pool, floor hoops, serving a customer floor by floor, floor pips on the marker |
| `House/House.Windows.cs` | window placement and breaking (free: smashing is never a penalty); six window styles (`WinW`/`WinH`/`WinGlass`), a wide style takes two grid cells; the frames and panes are drawn in `House.Facade.cs` |
| `House/House.Beach.cs` | beach lots (sand, sea, dunes, palms, sunbathers, picnic, lifeguard, cabins) |
| `House/House.Shop.cs` | shops and restaurants |
| `House/House.Park.cs` | parks, basketball court, car park |
| `House/House.Fort.cs` | walled houses (a wall in front of the ground floor) and building sites (no ground floor) |
| `House/House.Effects.cs` | hit-zone glows, shimmer, sparkles |
| `House/House.Lighting.cs` | lamps, door light, window spill at night |
| `House/House.Doors.cs` | doorstep path, door parts, door colours |
| `House/House.Props.cs` | throw blockers, street lamps, fences |
| `House/House.Destructibles.cs` | crate/can pyramids, breakables, hearts |
| `House/House.Structure.cs` | roof, trim, house shell |
| `House/House.Pool.cs` | shared pooled helpers (`Part`, `Cube`, `Sphere`, `SetZone`, blobs) |

## Rules of thumb
- One hoop per house: lawn houses may get the wall hoop or a reverse basket (trampoline + ring), paved-yard houses may get the standing hoop (`Yard.Hoop`), never two. A customer's building with upper floors has its own hoop on the first floor (`House.Floors.cs`) instead, and a customer whose yard has a hoop stays one floor.
- Red and orange are reserved for mailboxes, markers and hearts.
- Never draw random numbers from the route's seeded generator for cosmetics (use `Hash01` of the position) or the Daily Route changes.
- A customer's house has 1-3 floors and is served only when every floor is (ground: any ground target; upper: every balcony, or the building's one hoop for all the balconies at once). Never add a second path that finishes a house; go through `HandleHit` / `HandleBalcony` / `HandleFloorHoop` so the floor state stays right.
