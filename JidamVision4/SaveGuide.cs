using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JidamVision4.Core;
using JidamVision4.Setting;
using JidamVision4.Util;

namespace JidamVision4
{
    public partial class SaveGuide : Form
    {
        public SaveGuide()
        {
            InitializeComponent();
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            // 현재 모델 저장
            var m = Global.Inst.InspStage.CurModel;
            if (string.IsNullOrWhiteSpace(m.ModelPath))
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.InitialDirectory = SettingXml.Inst.ModelDir;
                    sfd.Title = "모델 파일 저장";
                    sfd.Filter = "Model Files (*.xml)|*.xml";
                    sfd.DefaultExt = "xml";
                    sfd.AddExtension = true;

                    if (sfd.ShowDialog() == DialogResult.OK)
                        Global.Inst.InspStage.CurModel?.SaveAs(sfd.FileName);
                }
            }
            else
            {
                m.Save();
                SLogger.Write($"[Model] 저장: {m.ModelName} ({m.ModelPath})");
            }

            ModelCloser.CloseModelToEmpty();
            this.DialogResult = DialogResult.OK;
            this.Close(); // 폼 닫기
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            ModelCloser.CloseModelToEmpty();
            this.DialogResult = DialogResult.OK;
            this.Close(); // 폼 닫기
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; // 다이얼로그 결과를 Cancel로 반환
            this.Close();                            // 폼 닫기
            SLogger.Write("모델 닫기 취소");
        }
    }
}
