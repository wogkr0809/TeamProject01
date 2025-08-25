using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JidamVision4.Core;

namespace JidamVision4.Util
{
    public static class ModelCloser
    {
        public static void CloseModelToEmpty()
        {
            var stage = Global.Inst?.InspStage;
            var m = stage?.CurModel;
            if (m != null)
                m.Reset();
            stage.CurModel = null;



            SLogger.Write("모든 모델을 닫았습니다.");
        }
    }
}
