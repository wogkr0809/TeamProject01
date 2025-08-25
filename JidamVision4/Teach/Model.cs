using Common.Util.Helpers;
using JidamVision4.Core;
using JidamVision4.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static JidamVision4.MainForm;

namespace JidamVision4.Teach
{
    public class Model
    {
        //모델 정보 저장을 위해 추가한 프로퍼티
        public string ModelName { get; set; } = string.Empty;
        public string ModelInfo { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;

        public string InspectImagePath { get; set; } = "";

        [XmlElement("InspWindow")]
        public List<InspWindow> InspWindowList { get; set; }

        public Model()
        {
            InspWindowList = new List<InspWindow>();
        }

        public InspWindow AddInspWindow(InspWindowType windowType)
        {
            InspWindow inspWindow = InspWindowFactory.Inst.Create(windowType);
            InspWindowList.Add(inspWindow);

            return inspWindow;
        }

        public bool AddInspWindow(InspWindow inspWindow)
        {
            if (inspWindow is null)
                return false;

            InspWindowList.Add(inspWindow);

            return true;
        }

        public bool DelInspWindow(InspWindow inspWindow)
        {
            if (InspWindowList.Contains(inspWindow))
            {
                InspWindowList.Remove(inspWindow);
                return true;
            }
            return false;
        }

        public bool DelInspWindowList(List<InspWindow> inspWindowList)
        {
            int before = InspWindowList.Count;
            InspWindowList.RemoveAll(w => inspWindowList.Contains(w));
            return InspWindowList.Count < before;
        }

        //신규 모델 생성
        public void CreateModel(string path, string modelName, string modelInfo)
        {
            ModelPath = path;
            ModelName = modelName;
            ModelInfo = modelInfo;
        }


        //#12_MODEL SAVE#2 모델 파일 Load,Save,SaveAs
        //모델 로딩함수
        public Model Load(string path)
        {
            Model model = XmlHelper.LoadXml<Model>(path);
            if (model == null)
                return null;

            ModelPath = path;

            foreach (var window in model.InspWindowList)
            {
                window.LoadInspWindow(model);
            }

            return model;
        }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(ModelPath))
                return;

            // 폴더 보장
            string dir = Path.GetDirectoryName(ModelPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 모델 자체 저장
            XmlHelper.SaveXml(ModelPath, this);

            // 윈도우별 부가 데이터 저장
            foreach (var window in InspWindowList)
            {
                window.SaveInspWindow(this);
            }
            UiHelpers.UpdateUiByModelStateSafe();
        }

        public bool SaveAs(string selectedPath)
        {
            if (string.IsNullOrWhiteSpace(selectedPath))
                return false;

            // 1) 사용자가 고른 경로를 정리(.xml 보장)
            string path = selectedPath.Trim();
            if (!string.Equals(Path.GetExtension(path), ".xml", StringComparison.OrdinalIgnoreCase))
                path += ".xml";

            string pickedDir = Path.GetDirectoryName(Path.GetFullPath(path)); // 대화상자에서 고른 폴더
            string pickedName = Path.GetFileNameWithoutExtension(path);        // 입력한 파일명 = 모델명

            // 2) 요구사항 구조:  ...\{pickedName}\{pickedName}.xml + Images
            string targetRootDir = Path.Combine(pickedDir, pickedName);
            string targetXmlPath = Path.Combine(targetRootDir, pickedName + ".xml");

            // 폴더 보장
            Directory.CreateDirectory(targetRootDir);
            Directory.CreateDirectory(Path.Combine(targetRootDir, "Images"));

            // 3) 잠깐만 경로/이름 바꿔서 저장 → 끝나면 원복 (현재 모델 유지)
            string oldPath = ModelPath;
            string oldName = ModelName;

            try
            {
                ModelPath = targetXmlPath;
                ModelName = pickedName;
                Save();                   // Save()는 내부 데이터/윈도우까지 저장
                SLogger.Write($"[Model] 다른 이름으로 저장: {pickedName} ({targetXmlPath})");
                return true;
            }
            finally
            {
                ModelPath = oldPath;
                ModelName = oldName;
            }
        }

        public void Reset()
        {
            ModelPath = string.Empty;
            ModelName = string.Empty;
            InspWindowList?.Clear();
        }
    }
}
