using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SaintCoinach.Graphics;
using SaintCoinach.Graphics.Viewer;
using SaintCoinach;
using SaintCoinach.Xiv;
using Aspose;
using Aspose.ThreeD;
using Aspose.ThreeD.Utilities;
using Aspose.ThreeD.Shading;

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

            Scene aspScene = new Scene();
            Node aspRoot = aspScene.RootNode.CreateChildNode(territory.Name);
            Node aspLgb = aspRoot.CreateChildNode("terrain");
            Node aspModel = new Node();

            Node aspSgb;

            PhongMaterial aspMat = new Aspose.ThreeD.Shading.PhongMaterial();
            Aspose.ThreeD.Entities.Mesh aspMesh = new Aspose.ThreeD.Entities.Mesh();

            var fformat = FileFormat.FBX7400ASCII;


            var customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            string teriName = territory.Name;

            try {
                string currentTitle = "";
                var _ExportDirectory = $"./{territory.Name}/";
                Dictionary<string, int> objCount = new Dictionary<string, int>();
                if (!System.IO.Directory.Exists(Environment.CurrentDirectory + $"{_ExportDirectory}")) {
                    System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + $"{_ExportDirectory}");
                }

                //var teriFileName = $"./{_ExportDirectory}/{territory.Name}.obj";
                var lightsFileName = $"./{_ExportDirectory}/{territory.Name}-lights.txt";
                var fileName = lightsFileName;

                var _ExportFileName = lightsFileName;
                {
                    var f = System.IO.File.Create(fileName);
                    f.Close();
                }
                System.IO.File.AppendAllText(fileName, $"o {territory.Name}\n");
                System.IO.File.WriteAllText(lightsFileName, "");
                int lights = 0;
                List<string> lightStrs = new List<string>() { "import bpy" };
                List<string> vertStr = new List<string>();
                Dictionary<string, Aspose.ThreeD.Shading.PhongMaterial> exportedMats = new Dictionary<string, Aspose.ThreeD.Shading.PhongMaterial>();
                UInt64 vs = 0, vt = 0, vn = 0, i = 0;
                Matrix IdentityMatrix = Matrix.Identity;

                void ExportMaterials(Material m, string path) {
                    
                    Aspose.ThreeD.Shading.PhongMaterial found = null;
                    if (exportedMats.TryGetValue(path, out found)) {
                        aspModel.Materials.Add(found);
                        return;
                    }

                    aspMat = new Aspose.ThreeD.Shading.PhongMaterial(path);

                    bool imgFound = false;
                    foreach (var img in m.TexturesFiles) {
                        
                        var mtlName = img.Path.Replace('/', '_');

                        SaintCoinach.Imaging.ImageConverter.Convert(img).Save($"{_ExportDirectory}/{mtlName}.png");
                        
                        var pngFile = mtlName + ".png";

                        if (mtlName.Contains("_dummy_"))
                            continue;

                        Aspose.ThreeD.Shading.Texture tex = new Aspose.ThreeD.Shading.Texture(pngFile);
                        tex.FileName = pngFile;
                        tex.Content = System.IO.File.ReadAllBytes($"{_ExportDirectory}/{mtlName}.png");

                        if (mtlName.Contains("_n.tex")) {
                            imgFound = true;
                            aspMat.SetTexture(Aspose.ThreeD.Shading.Material.MapNormal, tex);

                        }
                        else if (mtlName.Contains("_s.tex")) {
                            imgFound = true;

                            aspMat.SetTexture(Aspose.ThreeD.Shading.Material.MapSpecular, tex);
                        }
                        else if (!mtlName.Contains("_a.tex")) {
                            imgFound = true;

                            aspMat.SetTexture(Aspose.ThreeD.Shading.Material.MapDiffuse, tex);
                        }
                        else {
                            imgFound = true;
                            aspMat.SetTexture(Aspose.ThreeD.Shading.Material.MapAmbient, tex);
                        }

                    }

                    if (!imgFound)
                        return;

                    aspModel.Materials.Add(aspMat);
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

                    aspMesh = new Aspose.ThreeD.Entities.Mesh($"{modelFilePath}_{i}");

                    var tempVs = 0;

                    aspMesh.CreatePolygon(mesh.Indices.Select<ushort, int>(f => f).ToArray());

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

                        tempVs++;
                        aspMesh.ControlPoints.Add(new Aspose.ThreeD.Utilities.Vector4(x, y, z, w));

                        if (v.Color != null) {
                            var aspCol = new Aspose.ThreeD.Entities.VertexElementVertexColor();
                            aspCol.Data.Add(new Aspose.ThreeD.Utilities.Vector4(v.Color.Value.X, v.Color.Value.Y, v.Color.Value.Z, v.Color.Value.W));
                            aspMesh.VertexElements.Add(aspCol);
                            //aspCol.SetIndices(mesh.Indices.Select<ushort, int>(f => f).ToArray());
                        }
                        if (v.Normal != null) {
                            var aspNormal = new Aspose.ThreeD.Entities.VertexElementNormal();
                            aspNormal.Data.Add(new Aspose.ThreeD.Utilities.Vector4(v.Normal.Value.X, v.Normal.Value.Y, v.Normal.Value.Z, 0));
                            aspMesh.VertexElements.Add(aspNormal);
                            //aspNormal.SetIndices(mesh.Indices.Select<ushort, int>(f => f).ToArray());

                        }
                        if (v.UV != null) {
                            var aspUV = new Aspose.ThreeD.Entities.VertexElementUV();
                            aspUV.Data.Add(new Aspose.ThreeD.Utilities.Vector4(v.UV.Value.X, v.UV.Value.Y, v.UV.Value.Z, v.UV.Value.W));
                            aspMesh.VertexElements.Add(aspUV);
                            //aspUV.SetIndices(mesh.Indices.Select<ushort, int>(f => f).ToArray());

                        }
                        if (v.Tangent1 != null) {
                            //var aspTangent = new Aspose.ThreeD.Entities.VertexElementTangent();
                            //aspTangent.Data.Add(new Aspose.ThreeD.Utilities.Vector4(v.Tangent1.Value.X, v.Tangent1.Value.Y, v.Tangent1.Value.Z, v.Tangent1.Value.W));
                            //aspMesh.VertexElements.Add(aspTangent);
                        }
                        if (v.Tangent2 != null) {
                            //var aspTangent = new Aspose.ThreeD.Entities.VertexElementTangent();
                            //aspTangent.Data.Add(new Aspose.ThreeD.Utilities.Vector4(v.Tangent2.Value.X, v.Tangent2.Value.Y, v.Tangent2.Value.Z, v.Tangent2.Value.W));
                            //aspMesh.VertexElements.Add(aspTangent);
                        }

                    }
                    if (tempVs > 0) {
                        Aspose.ThreeD.Entities.PolygonModifier.Triangulate(aspMesh);
                        aspModel.AddEntity(aspMesh);
                    }
                }

                Dictionary<string, bool> exportedSgbFiles = new Dictionary<string, bool>();
                void ExportSgbModels(SaintCoinach.Graphics.Sgb.SgbFile sgbFile, ref Matrix lgbTransform, ref Matrix rootGimTransform, ref Matrix currGimTransform) {
                    foreach (var sgbGroup in sgbFile.Data.OfType<SaintCoinach.Graphics.Sgb.SgbGroup>()) {
                        bool newGroup = true;
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
                            var modelName = filePath.Substring(filePath.LastIndexOf("/") + 1) + "_" + i++;
                            aspModel = aspSgb.CreateChildNode(modelName);
                            
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
                            var transform = (Matrix.Translation(pos.X, pos.Y, pos.Z) * (rootGimTransform * currGimTransform) * lgbTransform).TranslationVector;
                            pos.X = transform.X;
                            pos.Y = transform.Y;
                            pos.Z = transform.Z;

                            lightStrs.Add($"#LIGHT_{lights++}_{light.Name}_{light.Header.UnknownId}");
                            lightStrs.Add($"#pos {pos.X} {pos.Y} {pos.Z}");
                            lightStrs.Add($"#UNKNOWNFLAGS 0x{light.Header.UnknownFlag1:X8} 0x{light.Header.UnknownFlag2:X8} 0x{light.Header.UnknownFlag3:X8} 0x{light.Header.UnknownFlag4:X8}");
                            lightStrs.Add($"#UNKNOWN {light.Header.Rotation.X} {light.Header.Rotation.Y} {light.Header.Rotation.Z}");
                            lightStrs.Add($"#UNKNOWN2 {light.Header.Scale.X} {light.Header.Scale.Y} {light.Header.Scale.Z}");
                            lightStrs.Add($"#unk {light.Header.Entry1.X} {light.Header.Entry1.Y}");
                            lightStrs.Add($"#unk2 {light.Header.Entry2.X} {light.Header.Entry2.Y}");
                            lightStrs.Add($"#unk3 {light.Header.Entry3.X} {light.Header.Entry3.Y}");
                            lightStrs.Add($"#unk4 {light.Header.Entry4.X} {light.Header.Entry4.Y}");
                            lightStrs.Add("");
                        }
                    }
                }

                progress.ReportProgress(0, currentTitle = "Terrain", "");
                if (territory.Terrain != null) {

                    var teriPart = 0;
                    foreach (var part in territory.Terrain.Parts) {
                        var hq = part.Model.GetModel(ModelQuality.High);
                        var filePath = hq.Definition.File.Path;
                        var lgbTransform = CreateMatrix(part.Translation, part.Rotation, part.Scale);

                        progress.ReportProgress(0, currentTitle, part.Model.File.Path);

                        aspModel = aspLgb.CreateChildNode(teriName + "_tr" + teriPart++);
                        for (var j = 0; j < hq.Meshes.Length; ++j) {
                            var mesh = hq.Meshes[j];
                            var mtl = mesh.Material.Get();
                            var path = mtl.File.Path.Replace('/', '_').Replace(".mtrl", ".tex");

                            ExportMaterials(mtl, path);
                            ExportMesh(ref mesh, ref lgbTransform, ref path, ref filePath, ref IdentityMatrix, ref IdentityMatrix, ref IdentityMatrix);
                        }
                    }
                }

                //aspScene.Save($"{_ExportDirectory}{teriName}" + fformat.Extension, fformat);

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

                                // new node
                                aspLgb = aspScene.RootNode.CreateChildNode(lgbGroup.Name);
                                //exportedMats.Clear();

                                lightsFileName = $"./{_ExportDirectory}/{teriName}-{lgbGroup.Name}-lights.txt";

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

                                    // new node
                                    aspModel = aspLgb.CreateChildNode(filePath.Substring(filePath.LastIndexOf("/") + 1) + "_" + i++);

                                    for (var j = 0; j < hq.Meshes.Length; ++j) {
                                        var mesh = hq.Meshes[j];
                                        var mtl = mesh.Material.Get();
                                        var path = mtl.File.Path.Replace('/', '_').Replace(".mtrl", ".tex");

                                        ExportMaterials(mtl, path);
                                        ExportMesh(ref mesh, ref lgbTransform, ref path, ref filePath, ref IdentityMatrix, ref IdentityMatrix, ref IdentityMatrix);
                                    }

                                    break;
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.Gimmick:
                                    var asGim = part as SaintCoinach.Graphics.Lgb.LgbGimmickEntry;
                                    if (asGim.Gimmick == null)
                                        continue;

                                    // new node
                                    aspSgb = aspLgb.CreateChildNode(asGim.Gimmick.File.Path);

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

                                    // new node
                                    aspSgb = aspLgb.CreateChildNode(asEobj.Gimmick.File.Path);


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
                                        foreach (var sgb1CEntry in rootGimGroup.Entries.OfType<SaintCoinach.Graphics.Sgb.SgbGroup1CEntry>()) {
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
                                    }

                                    break;
                                case SaintCoinach.Graphics.Lgb.LgbEntryType.Light:
                                    var asLight = part as SaintCoinach.Graphics.Lgb.LgbLightEntry;
                                    lightStrs.Add($"#LIGHT_{lights++}_{asLight.Name}_{asLight.Header.UnknownId}");
                                    lightStrs.Add($"#pos {asLight.Header.Translation.X} {asLight.Header.Translation.Y} {asLight.Header.Translation.Z}");
                                    lightStrs.Add($"#UNKNOWNFLAGS 0x{asLight.Header.UnknownFlag1:X8} 0x{asLight.Header.UnknownFlag2:X8} 0x{asLight.Header.UnknownFlag3:X8} 0x{asLight.Header.UnknownFlag4:X8}");
                                    lightStrs.Add($"#UNKNOWN {asLight.Header.Rotation.X} {asLight.Header.Rotation.Y} {asLight.Header.Rotation.Z}");
                                    lightStrs.Add($"#UNKNOWN2 {asLight.Header.Scale.X} {asLight.Header.Scale.Y} {asLight.Header.Scale.Z}");
                                    lightStrs.Add($"#unk {asLight.Header.Entry1.X} {asLight.Header.Entry1.Y}");
                                    lightStrs.Add($"#unk2 {asLight.Header.Entry2.X} {asLight.Header.Entry2.Y}");
                                    lightStrs.Add($"#unk3 {asLight.Header.Entry3.X} {asLight.Header.Entry3.Y}");
                                    lightStrs.Add($"#unk4 {asLight.Header.Entry4.X} {asLight.Header.Entry4.Y}");
                                    lightStrs.Add("");
                                    break;
                            }
                        }
                        System.IO.File.AppendAllLines(lightsFileName, lightStrs);
                        lightStrs.Clear();
                    }
                }

                aspScene.Save($"{_ExportDirectory}{teriName}" + fformat.Extension, fformat);
                //exporter.ExportFile(scene, $"{_ExportDirectory}{scene.RootNode.Name}." + exportFormat, exportId, PostProcessSteps.None);


                //System.IO.File.AppendAllLines(_ExportFileName, vertStr);
                //vertStr.Clear();
                //System.IO.File.AppendAllLines(lightsFileName, lightStrs);
                //lightStrs.Clear();
                System.Windows.Forms.MessageBox.Show("Finished exporting " + territory.Name + fformat.Extension, "", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (ExportCancelException e) {
                System.Windows.Forms.MessageBox.Show(e.Message, $"Canceled {teriName} export");
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
                System.Windows.Forms.MessageBox.Show(e.StackTrace, $"Unable to export {teriName}");
            }
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.DefaultThreadCurrentCulture;
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
