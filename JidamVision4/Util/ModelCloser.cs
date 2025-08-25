using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JidamVision4.Core;
using JidamVision4.UIControl;
using static JidamVision4.MainForm;

namespace JidamVision4.Util
{
    public static class ModelCloser
    {
        public static void CloseModelToEmpty()
        {
            var stage = Global.Inst?.InspStage;
            if (stage == null) return;

            stage.ResetToEmptyModel();

            // UI 정리: 각 폼에 간단한 Clear 유틸이 있다고 가정
            MainForm.GetDockForm<ModelTreeForm>()?.UpdateDiagramEntity();
            MainForm.GetDockForm<CameraForm>()?.SwitchToCurrentModel();
            MainForm.GetDockForm<ResultForm>()?.ClearAll();
            MainForm.GetDockForm<PropertiesForm>()?.ResetProperty();


            // (선택) 메뉴/버튼 비활성화 갱신
            UiHelpers.UpdateUiByModelStateSafe();

            SLogger.Write("모델을 닫았습니다.");
        }
    }
}
