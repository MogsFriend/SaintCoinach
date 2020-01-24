using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SaintCoinach.Graphics;
using SaintCoinach.Graphics.Viewer;
using SaintCoinach;
using SaintCoinach.Xiv;

namespace Godbert.ViewModels {
    using Commands;
    using SharpDX;
    using SaintCoinach.Graphics;

    public class TerritoryViewModel : ObservableBase {
        class ExportCancelException : Exception {
            public ExportCancelException(string message) : base(message) {

            }
        }
        #region Fields
        private TerritoryView[] _AllTerritories;
        private TerritoryView[] _FilteredTerritories;
        private TerritoryView _SelectedTerritory;
        private string _FilterTerm;
        private Ookii.Dialogs.Wpf.ProgressDialog _Progress;
        #endregion

        #region Properties
        public MainViewModel Parent { get; private set; }
        public IEnumerable<TerritoryView> AllTerritories { get { return _AllTerritories; } }
        public IEnumerable<TerritoryView> FilteredTerritories { get { return _FilteredTerritories; } }
        public TerritoryView SelectedTerritory {
            get { return _SelectedTerritory; }
            set {
                _SelectedTerritory = value;
                OnPropertyChanged(() => SelectedTerritory);
            }
        }
        public string FilterTerm {
            get { return _FilterTerm; }
            set {
                _FilterTerm = value;

                if (string.IsNullOrWhiteSpace(value))
                    _FilteredTerritories = _AllTerritories;
                else
                    _FilteredTerritories = _AllTerritories.Where(e => e.Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

                OnPropertyChanged(() => FilterTerm);
                OnPropertyChanged(() => FilteredTerritories);
            }
        }
        #endregion

        #region Constructor
        public TerritoryViewModel(MainViewModel parent) {
            this.Parent = parent;

            var allTerritoryTypes = parent.Realm.GameData.GetSheet<TerritoryType>();

            _AllTerritories = allTerritoryTypes
                .Where(t => !string.IsNullOrEmpty(t.Bg.ToString()))
                .Select(t => new TerritoryView(t))
                .OrderBy(m => m.PlaceNames)
                .ThenBy(m => m.TerritoryType.Key)
                .ToArray();

            _FilteredTerritories = _AllTerritories;
        }
        #endregion

        #region Commands
        private ICommand _OpenCommand;
        private ICommand _ExportCommand;

        public ICommand OpenCommand { get { return _OpenCommand ?? (_OpenCommand = new DelegateCommand(OnOpen)); } }
        public ICommand ExportCommand { get { return _ExportCommand ?? (_ExportCommand = new DelegateCommand(OnExport)); } }
        private void OnOpen() {
            if (SelectedTerritory == null)
                return;

            try {
                var t = new Territory(SelectedTerritory.TerritoryType);

                if (t == null)
                    System.Windows.MessageBox.Show(string.Format("Could not find territory data for {0}.", SelectedTerritory.Name), "Territory not found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                else
                    Parent.EngineHelper.OpenInNew(SelectedTerritory.Name, (e) => new SaintCoinach.Graphics.Viewer.Content.ContentTerritory(e, t));
            } catch (Exception e) {
                System.Windows.MessageBox.Show(string.Format("Error reading territory for {0}:{1}{2}", SelectedTerritory.Name, Environment.NewLine, e), "Failure to read territory", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnExport() {
            
            if (SelectedTerritory == null)
                return;
            try {
                Territory territory = new Territory(SelectedTerritory.TerritoryType);
                if (territory == null) {
                    System.Windows.MessageBox.Show($"Could not find territory data for {SelectedTerritory.Name}", "Territory not found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                _Progress = new Ookii.Dialogs.Wpf.ProgressDialog() {
                    WindowTitle = "Exporting territory " + territory.Name
                };
                _Progress.Show();
                _Progress.DoWork += (object sender, System.ComponentModel.DoWorkEventArgs eventArgs) => {
                    _Export(territory, (Ookii.Dialogs.Wpf.ProgressDialog)sender);
                };
            }
            catch (Exception e) {
                System.Windows.MessageBox.Show(string.Format("Error reading territory for {0}:{1}{2}", SelectedTerritory.Name, Environment.NewLine, e), "Failure to read territory", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            _Progress = null;
        }
        private void _Export(Territory territory, Ookii.Dialogs.Wpf.ProgressDialog progress) {

            string teriName = territory.Name;

            try {
                string currentTitle = "";
                var _ExportDirectory = $"./{territory.Name}/";
                Dictionary<string, int> objCount = new Dictionary<string, int>();
                if (!System.IO.Directory.Exists(Environment.CurrentDirectory + $"{_ExportDirectory}")) {
                    System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + $"{_ExportDirectory}");
                }

                var teriFileName = $"./{_ExportDirectory}/{territory.Name}.obj";
                var fileName = teriFileName;
                var lightsFileName = $"./{_ExportDirectory}/{territory.Name}-lights.txt";
                var _ExportFileName = fileName;
                {
                    var f = System.IO.File.Create(fileName);
                    f.Close();
                }
                System.IO.File.AppendAllText(fileName, $"o {territory.Name}\n");
                System.IO.File.WriteAllText(lightsFileName, "");
                int lights = 0;
                List<string> lightStrs = new List<string>() { "import bpy" };
                List<string> vertStr = new List<string>();
                Dictionary<string, bool> exportedPaths = new Dictionary<string, bool>();
                UInt64 vs = 1, vt = 1, vn = 1, i = 0;
                Matrix IdentityMatrix = Matrix.Identity;

                void ExportMaterials(Material m, string path) {
                    if (true)
                        return;
                    vertStr.Add($"mtllib {path}.mtl");
                    bool found = false;
                    if (exportedPaths.TryGetValue(path, out found)) {
                        return;
                    }
                    exportedPaths.Add(path, true);
                    System.IO.File.Delete($"{_ExportDirectory}/{path}.mtl");
                    System.IO.File.AppendAllText($"{_ExportDirectory}/{path}.mtl", $"newmtl {path}\n");
                    foreach (var img in m.TexturesFiles) {
                        var mtlName = img.Path.Replace('/', '_');
                        if (exportedPaths.TryGetValue(path + mtlName, out found)) {
                            continue;
                        }

                        //SaintCoinach.Imaging.ImageConverter.Convert(img).Save($"{_ExportDirectory}/{mtlName}.png");

                        if (mtlName.Contains("_dummy_"))
                            continue;

                        var ddsBytes = SaintCoinach.Imaging.ImageConverter.GetDDS(img);

                        var fileExt = ddsBytes != null ? ".dds" : ".png";
                        
                        if (fileExt == ".dds")
                            System.IO.File.WriteAllBytes($"{_ExportDirectory}/{mtlName}.dds", ddsBytes);
                        else
                            SaintCoinach.Imaging.ImageConverter.Convert(img).Save($"{_ExportDirectory}/{mtlName}.png");


                        if (mtlName.Contains("_n.tex")) {
                            System.IO.File.AppendAllText($"./{_ExportDirectory}/{path}.mtl", $"bump {mtlName}{fileExt}\n");
                        }
                        else if (mtlName.Contains("_s.tex")) {
                            System.IO.File.AppendAllText($"./{_ExportDirectory}/{path}.mtl", $"map_Ks {mtlName}{fileExt}\n");
                        }
                        else if (!mtlName.Contains("_a.tex")) {
                            System.IO.File.AppendAllText($"./{_ExportDirectory}/{path}.mtl", $"map_Kd {mtlName}{fileExt}\n");
                        }
                        else {
                            System.IO.File.AppendAllText($"./{_ExportDirectory}/{path}.mtl", $"map_Ka {mtlName}{fileExt}\n");
                        }

                        exportedPaths.Add(path + mtlName, true);
                    }
                }

                Matrix CreateMatrix(SaintCoinach.Graphics.Vector3 translation, SaintCoinach.Graphics.Vector3 rotation, SaintCoinach.Graphics.Vector3 scale) {
                    return (Matrix.Scaling(scale.ToDx())
                        * Matrix.RotationX(rotation.X)
                        * Matrix.RotationY(rotation.Y)
                        * Matrix.RotationZ(rotation.Z)
                        * Matrix.Translation(translation.ToDx()));
                }

                void ExportMesh(ref Mesh mesh, ref Matrix lgbTransform, ref string materialName, ref string modelFilePath,
                    ref Matrix rootGimTransform, ref Matrix currGimTransform, ref Matrix modelTransform) {
                    i++;
                    if (progress.CancellationPending)
                        throw new ExportCancelException("User canceled export");
                    if (true)
                        return;
                    var k = 0;
                    UInt64 tempVs = 0, tempVn = 0, tempVt = 0;
                    foreach (var v in mesh.Vertices) {

                        var x = v.Position.Value.X;
                        var y = v.Position.Value.Y;
                        var z = v.Position.Value.Z;
                        var w = v.Position.Value.W;

                        var transform = (modelTransform * rootGimTransform * currGimTransform) * lgbTransform;

                        var t = Matrix.Translation(x, y, z) * transform;
                        x = t.TranslationVector.X;
                        y = t.TranslationVector.Y;
                        z = t.TranslationVector.Z;

                        // .Replace(',','.') cause decimal separator locale memes
                        if (v.Color != null)
                            vertStr.Add($"v {x} {y} {z} {v.Color.Value.X} {v.Color.Value.Y} {v.Color.Value.Z} {v.Color.Value.W}".Replace(',','.'));
                        else
                            vertStr.Add($"v {x} {y} {z}".Replace(',','.'));

                        tempVs++;

                        vertStr.Add($"vn {v.Normal.Value.X} {v.Normal.Value.Y} {v.Normal.Value.Z}".Replace(',', '.'));
                        tempVn++;

                        if (v.UV != null) {
                            vertStr.Add($"vt {v.UV.Value.X} {v.UV.Value.Y * -1.0}".Replace(',', '.'));
                            tempVt++;
                        }
                    }
                    vertStr.Add($"g {modelFilePath}_{i.ToString()}_{k.ToString()}");
                    vertStr.Add($"usemtl {materialName}");
                    for (UInt64 j = 0; j + 3 < (UInt64)mesh.Indices.Length + 1; j += 3) {
                        vertStr.Add(
                            $"f " +
                            $"{mesh.Indices[j] + vs}/{mesh.Indices[j] + vt}/{mesh.Indices[j] + vn} " +
                            $"{mesh.Indices[j + 1] + vs}/{mesh.Indices[j + 1] + vt}/{mesh.Indices[j + 1] + vn} " +
                            $"{mesh.Indices[j + 2] + vs}/{mesh.Indices[j + 2] + vt}/{mesh.Indices[j + 2] + vn}");
                    }
                    if (i % 1000 == 0) {
                        System.IO.File.AppendAllLines(_ExportFileName, vertStr);
                        vertStr.Clear();
                    }
                    vs += tempVs;
                    vn += tempVn;
                    vt += tempVt;
                }

                Dictionary<string, bool> exportedSgbFiles = new Dictionary<string, bool>();
                void ExportSgbModels(SaintCoinach.Graphics.Sgb.SgbFile sgbFile, ref Matrix lgbTransform, ref Matrix rootGimTransform, ref Matrix currGimTransform) {
                    foreach (var sgbGroup in sgbFile.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                        bool newGroup = true;

                        foreach (var sgb1CEntry in sgbGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGroup1CEntry>()) {
                            if (sgb1CEntry.Gimmick != null) {
                                ExportSgbModels(sgb1CEntry.Gimmick, ref lgbTransform, ref IdentityMatrix, ref IdentityMatrix);
                                foreach (var subGimGroup in sgb1CEntry.Gimmick.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                                    foreach (var subGimEntry in subGimGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGimmickEntry>()) {
                                        var subGimTransform = CreateMatrix(subGimEntry.Header.Translation, subGimEntry.Header.Rotation, subGimEntry.Header.Scale);
                                        ExportSgbModels(subGimEntry.Gimmick, ref lgbTransform, ref IdentityMatrix, ref subGimTransform);
                                    }
                                }
                            }
                        }

                        foreach (var mdl in sgbGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbModelEntry>()) {
                            Model hq = null;
                            var filePath = mdl.ModelFilePath;
                            var modelTransform = CreateMatrix(mdl.Header.Translation, mdl.Header.Rotation, mdl.Header.Scale);

                            progress.ReportProgress(0, currentTitle, filePath);
                            try {
                                hq = mdl.Model.Model.GetModel(ModelQuality.High);
                            }
                            catch (Exception e) {
                                System.Diagnostics.Debug.WriteLine($"Unable to load model for {mdl.Name} path: {filePath}.  Exception: {e.Message}");
                                continue;
                            }
                            if (newGroup) {
                                //vertStr.Add($"o {sgbFile.File.Path}_{sgbGroup.Name}_{i}");
                                newGroup = false;
                            }
                            for (var j = 0; j < hq.Meshes.Length; ++j) {
                                var mesh = hq.Meshes[j];
                                var mtl = mesh.Material.Get();
                                var path = mtl.File.Path.Replace('/', '_').Replace(".mtrl", ".tex");

                                ExportMaterials(mtl, path);
                                ExportMesh(ref mesh, ref lgbTransform, ref path, ref filePath, ref rootGimTransform, ref currGimTransform, ref modelTransform);
                            }
                        }

                        foreach (var light in sgbGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbLightEntry>()) {
                            var pos = light.Header.Translation;
                            var transform = (rootGimTransform * currGimTransform) * lgbTransform;

                            var t = Matrix.Translation(pos.X, pos.Y, pos.Z) * transform;
                            pos.X = t.TranslationVector.X;
                            pos.Y = t.TranslationVector.Y;
                            pos.Z = t.TranslationVector.Z;

                            var lHdr = light.Header;

                            lightStrs.Add($"SGB_LIGHT_{lights++}_{light.Name}_{light.Header.UnknownId}");
                            lightStrs.Add($"Pos {pos.X} {pos.Y} {pos.Z}");
                            lightStrs.Add($"Rot {light.Header.Rotation.X} {light.Header.Rotation.Y} {light.Header.Rotation.Z}");
                            lightStrs.Add($"Scale {light.Header.Scale.X} {light.Header.Scale.Y} {light.Header.Scale.Z}");
                            lightStrs.Add($"LightType {lHdr.LightType.ToString()}");
                            lightStrs.Add($"Attenuation {lHdr.Attenuation}");
                            lightStrs.Add($"RangeRate {lHdr.RangeRate}");
                            lightStrs.Add($"PointLightType {lHdr.PointLightType.ToString()}");
                            lightStrs.Add($"AttenuationConeCoefficient {lHdr.AttenuationConeCoefficient}");
                            lightStrs.Add($"ConeDegree {lHdr.ConeDegree}");
                            lightStrs.Add($"TexturePath {light.TexturePath}");
                            lightStrs.Add($"ColorHDRI {lHdr.DiffuseColorHDRI.Red} {lHdr.DiffuseColorHDRI.Green} {lHdr.DiffuseColorHDRI.Blue} {lHdr.DiffuseColorHDRI.Alpha} {lHdr.DiffuseColorHDRI.Intensity}");
                            lightStrs.Add($"FollowsDirectionalLight {lHdr.FollowsDirectionalLight}");
                            lightStrs.Add($"SpecularEnabled {lHdr.SpecularEnabled}");
                            lightStrs.Add($"BGShadowEnabled {lHdr.BGShadowEnabled}");
                            lightStrs.Add($"CharacterShadowEnabled {lHdr.CharacterShadowEnabled}");
                            lightStrs.Add($"ShadowClipRange {lHdr.ShadowClipRange}");
                            lightStrs.Add($"PlaneLightRotationX {lHdr.PlaneLightRotationX}");
                            lightStrs.Add($"PlaneLightRotationY {lHdr.PlaneLightRotationY}");
                            lightStrs.Add($"MergeGroupID {lHdr.MergeGroupID}");
                            lightStrs.Add("");
                        }

                        foreach (var asVfx in sgbGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbVfxEntry>()) {
                            var vHdr = asVfx.Header;
                            var pos = vHdr.Translation;
                            var transform = (rootGimTransform * currGimTransform) * lgbTransform;
                            var name = $"SGB_VFX_{lights++}_{asVfx.AvfxFile.Models.Count}_{asVfx.Header.UnknownId}";

                            var t = Matrix.Translation(pos.X, pos.Y, pos.Z) * transform;
                            pos.X = t.TranslationVector.X;
                            pos.Y = t.TranslationVector.Y;
                            pos.Z = t.TranslationVector.Z;

                            lightStrs.Add(name);
                            lightStrs.Add($"Pos {pos.X} {pos.Y} {pos.Z}");
                            lightStrs.Add($"Rot {asVfx.Header.Rotation.X} {asVfx.Header.Rotation.Y} {asVfx.Header.Rotation.Z}");
                            lightStrs.Add($"Scale {asVfx.Header.Scale.X} {asVfx.Header.Scale.Y} {asVfx.Header.Scale.Z}");
                            lightStrs.Add($"LightType {"VFX"}");
                            lightStrs.Add($"Attenuation {"1"}");
                            lightStrs.Add($"RangeRate {"1"}");
                            lightStrs.Add($"PointLightType {"Point"}");
                            lightStrs.Add($"AttenuationConeCoefficient {"1"}");
                            lightStrs.Add($"ConeDegree {"45"}");
                            lightStrs.Add($"TexturePath {asVfx.FilePath}");
                            lightStrs.Add($"ColorHDRI {vHdr.Color.Red} {vHdr.Color.Green} {vHdr.Color.Blue} {vHdr.Color.Alpha} {"1.0"}");
                            lightStrs.Add($"FollowsDirectionalLight {"0"}");
                            lightStrs.Add($"SpecularEnabled {"1"}");
                            lightStrs.Add($"BGShadowEnabled {"1"}");
                            lightStrs.Add($"CharacterShadowEnabled {"0"}");
                            lightStrs.Add($"ShadowClipRange {"10"}");
                            lightStrs.Add($"PlaneLightRotationX {"0"}");
                            lightStrs.Add($"PlaneLightRotationY {"0"}");
                            lightStrs.Add($"MergeGroupID {"0"}");
                            List<string> vfxObjStr = new List<string>();
                            string path = name.Replace("/", "_");
                            string mtlName = path;
                            /*
                            int vfxPos = 0, vfxN = 0, vfxT = 0, vfxC = 0;
                            for (int x = 0; x < asVfx.AvfxFile.Models.Count; ++x) {
                                var model = asVfx.AvfxFile.Models[x];


                                vfxObjStr.Add($"mtllib ..\\textures\\{path}.mtl");
                                foreach (var vertex in model.ConvertedVertexes) {
                                    vfxObjStr.Add($"v {vertex.Position.X} {vertex.Position.Y} {vertex.Position.Z}");
                                    vfxObjStr.Add($"vc {vertex.Color.W} {vertex.Color.X} {vertex.Color.Y} {vertex.Color.Z}");
                                    vfxObjStr.Add($"vt {vertex.UV1.X} {vertex.UV2.Y * -1.0f}");
                                    vfxObjStr.Add($"vn {vertex.Normal.X} {vertex.Normal.Y} {vertex.Normal.Z}");
                                }

                                vfxObjStr.Add($"g {name}_{x}");
                                vfxObjStr.Add($"usemtl {mtlName}");
                                foreach (var face in model.Indices) {
                                    int f1 = face.I1 + vfxPos + 1;
                                    int f2 = face.I2 + vfxPos + 1;
                                    int f3 = face.I3 + vfxPos + 1;

                                    vfxObjStr.Add($"f {f1}/{f1}/{f1} {f2}/{f2}/{f2} {f3}/{f3}/{f3}");
                                }
                                vfxPos += model.ConvertedVertexes.Length;
                            }
                            */
                            System.IO.File.WriteAllText($"./{_ExportDirectory}/{path}.mtl", $"newmtl {mtlName}\n");

                            for (int x = 0; x < asVfx.AvfxFile.Textures.Count; ++x) {

                                mtlName = asVfx.AvfxFile.TexturePaths[x].Replace("/", "_");
                                var img = asVfx.AvfxFile.Textures[x];

                                var ddsBytes = SaintCoinach.Imaging.ImageConverter.GetDDS(img);

                                var fileExt = ddsBytes != null ? ".dds" : ".png";

                                if (fileExt == ".dds")
                                    System.IO.File.WriteAllBytes($"{_ExportDirectory}/{mtlName}.dds", ddsBytes);
                                else
                                    SaintCoinach.Imaging.ImageConverter.Convert(img).Save($"{_ExportDirectory}/{mtlName}.png");

                                System.IO.File.AppendAllText($"./{_ExportDirectory}/{path}.mtl", $"map_Kd {mtlName}{fileExt}\n");
                            }

                            System.IO.File.WriteAllLines($"./{_ExportDirectory}/{name}.obj", vfxObjStr);

                            lightStrs.Add("");
                        }
                    }
                }

                progress.ReportProgress(0, currentTitle = "Terrain", "");
                if (territory.Terrain != null) {
                    foreach (var part in territory.Terrain.Parts) {
                        var hq = part.Model.GetModel(ModelQuality.High);
                        var filePath = hq.Definition.File.Path;
                        var lgbTransform = CreateMatrix(part.Translation, part.Rotation, part.Scale);

                        progress.ReportProgress(0, currentTitle, part.Model.File.Path);

                        for (var j = 0; j < hq.Meshes.Length; ++j) {
                            var mesh = hq.Meshes[j];
                            var mtl = mesh.Material.Get();
                            var path = mtl.File.Path.Replace('/', '_').Replace(".mtrl", ".tex");

                            ExportMaterials(mtl, path);
                            ExportMesh(ref mesh, ref lgbTransform, ref path, ref filePath, ref IdentityMatrix, ref IdentityMatrix, ref IdentityMatrix);
                        }
                    }
                }

                //System.IO.File.AppendAllLines(_ExportFileName, vertStr);
                vertStr.Clear();
                vs = 1; vn = 1; vt = 1; i = 0;
                int grp = 0;
                foreach (var lgb in territory.LgbFiles) {
                    foreach (var lgbGroup in lgb.Groups) {

                        bool newGroup = true;
                        foreach (var part in lgbGroup.Entries) {
                            if (part == null)
                                continue;

                            if (newGroup && (part.Type == SaintCoinach.Graphics.Lgb.LgbEntryType.Model || part.Type == SaintCoinach.Graphics.Lgb.LgbEntryType.Gimmick || part.Type == SaintCoinach.Graphics.Lgb.LgbEntryType.Light)) {
                                progress.WindowTitle = $"Exporting {territory.Name} ({lgbGroup.Name})";
                                progress.ReportProgress(0, currentTitle = $"Exporting {territory.Name} Group {lgbGroup.Name}", $"Group {lgbGroup.Name}");

                                newGroup = false;

                                //System.IO.File.AppendAllLines(_ExportFileName, vertStr);
                                System.IO.File.AppendAllLines(lightsFileName, lightStrs);

                                lightStrs.Clear();
                                vertStr.Clear();

                                //vertStr.Add($"o {lgbGroup.Name}");

                                vs = 1; vn = 1; vt = 1; i = 0;
                                _ExportFileName = $"./{_ExportDirectory}/{teriName}-{lgbGroup.Name}.obj";
                                lightsFileName = $"./{_ExportDirectory}/{teriName}-{lgbGroup.Name}_{++grp}-lights.txt";

                                var f = System.IO.File.Create(_ExportFileName);
                                f.Close();
                                f = System.IO.File.Create(lightsFileName);
                                f.Close();
                            }

                            switch (part.Type) {
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.Model:
                                    var asMdl = part as SaintCoinach.Graphics.Lgb.LgbModelEntry;
                                    progress.ReportProgress(0, currentTitle = "Exporting LgbModel", asMdl.ModelFilePath);

                                    if (asMdl.Model == null)
                                        continue;

                                    var hq = asMdl.Model.Model.GetModel(ModelQuality.High);
                                    var lgbTransform = CreateMatrix(asMdl.Header.Translation, asMdl.Header.Rotation, asMdl.Header.Scale);
                                    var filePath = asMdl.ModelFilePath;

                                    for (var j = 0; j < hq.Meshes.Length; ++j) {
                                        var mesh = hq.Meshes[j];
                                        var mtl = mesh.Material.Get();
                                        var path2 = mtl.File.Path.Replace('/', '_').Replace(".mtrl", ".tex");

                                        ExportMaterials(mtl, path2);
                                        ExportMesh(ref mesh, ref lgbTransform, ref path2, ref filePath, ref IdentityMatrix, ref IdentityMatrix, ref IdentityMatrix);
                                    }
                                    break;
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.Gimmick:
                                    var asGim = part as SaintCoinach.Graphics.Lgb.LgbGimmickEntry;
                                    if (asGim.Gimmick == null)
                                        continue;
                                               
                                    progress.ReportProgress(0, currentTitle = $"Exporting Gimmick {asGim.Name} {asGim.Header.GimmickId}", "");
                                    
                                    lgbTransform = CreateMatrix(asGim.Header.Translation, asGim.Header.Rotation, asGim.Header.Scale);

                                    ExportSgbModels(asGim.Gimmick, ref lgbTransform, ref IdentityMatrix, ref IdentityMatrix);
                                    foreach (var rootGimGroup in asGim.Gimmick.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                                        foreach (var rootGimEntry in rootGimGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGimmickEntry>()) {
                                            if (rootGimEntry.Gimmick != null) {
                                                var rootGimTransform = CreateMatrix(rootGimEntry.Header.Translation, rootGimEntry.Header.Rotation, rootGimEntry.Header.Scale);
                                                ExportSgbModels(rootGimEntry.Gimmick, ref lgbTransform, ref rootGimTransform, ref IdentityMatrix);
                                                foreach (var subGimGroup in rootGimEntry.Gimmick.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                                                    foreach (var subGimEntry in subGimGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGimmickEntry>()) {
                                                        var subGimTransform = CreateMatrix(subGimEntry.Header.Translation, subGimEntry.Header.Rotation, subGimEntry.Header.Scale);
                                                        ExportSgbModels(subGimEntry.Gimmick, ref lgbTransform, ref rootGimTransform, ref subGimTransform);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.EventObject:
                                    var asEobj = part as SaintCoinach.Graphics.Lgb.LgbEventObjectEntry;
                                    if (asEobj.Gimmick == null)
                                        continue;

                                    progress.ReportProgress(0, currentTitle = $"Exporting EObj {asEobj.Name} {asEobj.Header.EventObjectId} {asEobj.Header.GimmickId}", "");

                                    lgbTransform = CreateMatrix(asEobj.Header.Translation, asEobj.Header.Rotation, asEobj.Header.Scale);

                                    ExportSgbModels(asEobj.Gimmick, ref lgbTransform, ref IdentityMatrix, ref IdentityMatrix);
                                    foreach (var rootGimGroup in asEobj.Gimmick.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                                        foreach (var rootGimEntry in rootGimGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGimmickEntry>()) {
                                            if (rootGimEntry.Gimmick != null) {
                                                var rootGimTransform = CreateMatrix(rootGimEntry.Header.Translation, rootGimEntry.Header.Rotation, rootGimEntry.Header.Scale);
                                                ExportSgbModels(rootGimEntry.Gimmick, ref lgbTransform, ref rootGimTransform, ref IdentityMatrix);
                                                foreach (var subGimGroup in rootGimEntry.Gimmick.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                                                    foreach (var subGimEntry in subGimGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGimmickEntry>()) {
                                                        var subGimTransform = CreateMatrix(subGimEntry.Header.Translation, subGimEntry.Header.Rotation, subGimEntry.Header.Scale);
                                                        ExportSgbModels(subGimEntry.Gimmick, ref lgbTransform, ref rootGimTransform, ref subGimTransform);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.Light:
                                    var asLight = part as SaintCoinach.Graphics.Lgb.LgbLightEntry;
                                    var lHdr = asLight.Header;
                                    lightStrs.Add($"LGB_LIGHT_{lights++}_{asLight.Name}_{asLight.Header.UnknownId}");
                                    lightStrs.Add($"Pos {asLight.Header.Translation.X} {asLight.Header.Translation.Y} {asLight.Header.Translation.Z}");
                                    lightStrs.Add($"Rot {asLight.Header.Rotation.X} {asLight.Header.Rotation.Y} {asLight.Header.Rotation.Z}");
                                    lightStrs.Add($"Scale {asLight.Header.Scale.X} {asLight.Header.Scale.Y} {asLight.Header.Scale.Z}");
                                    lightStrs.Add($"LightType {lHdr.LightType.ToString()}");
                                    lightStrs.Add($"Attenuation {lHdr.Attenuation}");
                                    lightStrs.Add($"RangeRate {lHdr.RangeRate}");
                                    lightStrs.Add($"PointLightType {lHdr.PointLightType.ToString()}");
                                    lightStrs.Add($"AttenuationConeCoefficient {lHdr.AttenuationConeCoefficient}");
                                    lightStrs.Add($"ConeDegree {lHdr.ConeDegree}");
                                    lightStrs.Add($"TexturePath {asLight.TexturePath}");
                                    lightStrs.Add($"ColorHDRI {lHdr.DiffuseColorHDRI.Red} {lHdr.DiffuseColorHDRI.Green} {lHdr.DiffuseColorHDRI.Blue} {lHdr.DiffuseColorHDRI.Alpha} {lHdr.DiffuseColorHDRI.Intensity}");
                                    lightStrs.Add($"FollowsDirectionalLight {lHdr.FollowsDirectionalLight}");
                                    lightStrs.Add($"SpecularEnabled {lHdr.SpecularEnabled}");
                                    lightStrs.Add($"BGShadowEnabled {lHdr.BGShadowEnabled}");
                                    lightStrs.Add($"CharacterShadowEnabled {lHdr.CharacterShadowEnabled}");
                                    lightStrs.Add($"ShadowClipRange {lHdr.ShadowClipRange}");
                                    lightStrs.Add($"PlaneLightRotationX {lHdr.PlaneLightRotationX}");
                                    lightStrs.Add($"PlaneLightRotationY {lHdr.PlaneLightRotationY}");
                                    lightStrs.Add($"MergeGroupID {lHdr.MergeGroupID}");

                                    lightStrs.Add("");
                                    break;
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.Vfx:
                                    var asVfx = part as SaintCoinach.Graphics.Lgb.LgbVfxEntry;
                                    var vHdr = asVfx.Header;
                                    var name = $"LGB_VFX_{lights++}_{asVfx.AvfxFile.Models.Count}_{asVfx.Header.UnknownId}";

                                    lightStrs.Add(name);
                                    lightStrs.Add($"Pos {asVfx.Header.Translation.X} {asVfx.Header.Translation.Y} {asVfx.Header.Translation.Z}");
                                    lightStrs.Add($"Rot {asVfx.Header.Rotation.X} {asVfx.Header.Rotation.Y} {asVfx.Header.Rotation.Z}");
                                    lightStrs.Add($"Scale {asVfx.Header.Scale.X} {asVfx.Header.Scale.Y} {asVfx.Header.Scale.Z}");
                                    lightStrs.Add($"LightType {"VFX"}");
                                    lightStrs.Add($"Attenuation {"1"}");
                                    lightStrs.Add($"RangeRate {"1"}");
                                    lightStrs.Add($"PointLightType {"Point"}");
                                    lightStrs.Add($"AttenuationConeCoefficient {"1"}");
                                    lightStrs.Add($"ConeDegree {"45"}");
                                    lightStrs.Add($"TexturePath {asVfx.FilePath}");
                                    lightStrs.Add($"ColorHDRI {vHdr.Color.Red} {vHdr.Color.Green} {vHdr.Color.Blue} {vHdr.Color.Alpha} {"1.0"}");
                                    lightStrs.Add($"FollowsDirectionalLight {"0"}");
                                    lightStrs.Add($"SpecularEnabled {"1"}");
                                    lightStrs.Add($"BGShadowEnabled {"1"}");
                                    lightStrs.Add($"CharacterShadowEnabled {"0"}");
                                    lightStrs.Add($"ShadowClipRange {"10"}");
                                    lightStrs.Add($"PlaneLightRotationX {"0"}");
                                    lightStrs.Add($"PlaneLightRotationY {"0"}");
                                    lightStrs.Add($"MergeGroupID {"0"}");

                                    List<string> vfxObjStr = new List<string>();
                                    string path = name.Replace("/", "_");
                                    string mtlName = path;

                                    /*
                                    int vfxPos = 0, vfxN = 0, vfxT = 0, vfxC = 0;
                                    for (int x = 0; x < asVfx.AvfxFile.Models.Count; ++x) {
                                        var model = asVfx.AvfxFile.Models[x];


                                        vfxObjStr.Add($"mtllib ..\\textures\\{path}.mtl");
                                        foreach (var vertex in model.ConvertedVertexes) {
                                            vfxObjStr.Add($"v {vertex.Position.X} {vertex.Position.Y} {vertex.Position.Z}");
                                            vfxObjStr.Add($"vc {vertex.Color.W} {vertex.Color.X} {vertex.Color.Y} {vertex.Color.Z}");
                                            vfxObjStr.Add($"vt {vertex.UV1.X} {vertex.UV2.Y * -1.0f}");
                                            vfxObjStr.Add($"vn {vertex.Normal.X} {vertex.Normal.Y} {vertex.Normal.Z}");
                                        }

                                        vfxObjStr.Add($"g {name}_{x}");
                                        vfxObjStr.Add($"usemtl {mtlName}");
                                        foreach (var face in model.Indices) {
                                            int f1 = face.I1 + vfxPos + 1;
                                            int f2 = face.I2 + vfxPos + 1;
                                            int f3 = face.I3 + vfxPos + 1;

                                            vfxObjStr.Add($"f {f1}/{f1}/{f1} {f2}/{f2}/{f2} {f3}/{f3}/{f3}");
                                        }
                                        vfxPos += model.ConvertedVertexes.Length;
                                    }
                                    */
                                    System.IO.File.WriteAllText($"./{_ExportDirectory}/{path}.mtl", $"newmtl {mtlName}\n");

                                    for (int x = 0; x < asVfx.AvfxFile.Textures.Count; ++x) {

                                        mtlName = asVfx.AvfxFile.TexturePaths[x].Replace("/", "_");
                                        var img = asVfx.AvfxFile.Textures[x];

                                        var ddsBytes = SaintCoinach.Imaging.ImageConverter.GetDDS(img);

                                        var fileExt = ddsBytes != null ? ".dds" : ".png";

                                        if (fileExt == ".dds")
                                            System.IO.File.WriteAllBytes($"{_ExportDirectory}/{mtlName}.dds", ddsBytes);
                                        else
                                            SaintCoinach.Imaging.ImageConverter.Convert(img).Save($"{_ExportDirectory}/{mtlName}.png");

                                        System.IO.File.AppendAllText($"./{_ExportDirectory}/{path}.mtl", $"map_Kd {mtlName}{fileExt}\n");
                                    }

                                    System.IO.File.WriteAllLines($"./{_ExportDirectory}/{name}.obj", vfxObjStr);

                                    lightStrs.Add("");
                                    break;
                            }
                        }
                        System.IO.File.AppendAllLines(lightsFileName, lightStrs);
                        lightStrs.Clear();
                    }
                }
                System.IO.File.AppendAllLines(_ExportFileName, vertStr);
                vertStr.Clear();
                System.IO.File.AppendAllLines(lightsFileName, lightStrs);
                lightStrs.Clear();
                System.Windows.Forms.MessageBox.Show("Finished exporting " + territory.Name, "", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (ExportCancelException e) {
                System.Windows.Forms.MessageBox.Show(e.Message, $"Canceled {teriName} export");
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
                System.Windows.Forms.MessageBox.Show(e.StackTrace, $"Unable to export {teriName}");
            }
        }
        #endregion

        #region Refresh
        public void Refresh() {
            _FilteredTerritories = _FilteredTerritories.ToArray();
            OnPropertyChanged(() => FilteredTerritories);
        }
        #endregion
    }

    public class TerritoryView
    {
        public TerritoryView(TerritoryType territory)
        {
            TerritoryType = territory;

            var places = new List<string>();
            places.Add(territory.RegionPlaceName.Name.ToString());
            places.Add(territory.ZonePlaceName.Name.ToString());
            places.Add(territory.PlaceName.Name.ToString());
            PlaceNames = string.Join(" > ", places.Where(p => !string.IsNullOrEmpty(p)).Distinct());
            
            Name = string.Format("({0}) {1}", territory.Name.ToString(), PlaceNames);
        }

        public TerritoryType TerritoryType { get; private set; }
        public string Name { get; private set; }
        public string PlaceNames { get; private set; }
    }
}
