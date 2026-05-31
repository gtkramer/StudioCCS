// Project-wide imports for the regrouped namespace taxonomy. Declaring these
// once here keeps the (DI-less) codebase's import-free ergonomics after the
// reorganization, so consumer files don't each need a `using` for every area.
// See the namespace tree: StudioCCS.FileFormat[.Geometry|.Materials|.Animation|
// .SceneObjects|.Raw], StudioCCS.Rendering[.Gizmos], StudioCCS.Logging. The two
// shared tree-model types (CCSTreeNode, TreeNodeTag) remain in the root
// StudioCCS namespace.
global using StudioCCS.FileFormat;
global using StudioCCS.FileFormat.Animation;
global using StudioCCS.FileFormat.Geometry;
global using StudioCCS.FileFormat.Materials;
global using StudioCCS.FileFormat.Raw;
global using StudioCCS.FileFormat.SceneObjects;
global using StudioCCS.Logging;
global using StudioCCS.Rendering;
global using StudioCCS.Rendering.Gizmos;
