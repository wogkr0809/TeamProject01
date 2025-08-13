using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Teach;
using JidamVision4.UIControl;
using JidamVision4.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace JidamVision4
{
    /*
    #11_MODEL_TREE# - <<ROI를 관리하는 UI 구현>>> 
    모델 트리를 통해서, ROI를 추가하고, 관리하는 기능을 구현한다.
    1) ModelTreeForm WinForm 생성 후, ModelTree Control을 추가한다.       
    2) ModelTreeForm은 DockContent를 상속받아, MainForm에 도킹    
    2) #11_MODEL_TREE#1 ~ 3 구현
    */

    /*
    #MODEL TREE# - <<<ROI 티칭을 위한 모델트리 만들기>>> 
    다양한 타입의 ROI를 입력하고, 관리하기 위해, 계층 구조를 나타낼 수 있는
    TreeView 컨트롤을 이용해, ROI를 입력하는 기능 개발
    1) ModelTreeForm WindowForm 생성
    2) TreeView Control 추가
    3) name을 tvModelTree로 설정
    */

    public partial class ModelTreeForm: DockContent
    {
        //개별 트리 노트에서 팝업 메뉴 보이기를 위한 메뉴
        private ContextMenuStrip _contextMenu;

        public ModelTreeForm()
        {
            InitializeComponent();

            tvModelTree.CheckBoxes = true; //트리 노드에 체크박스 표시

            //초기 트리 노트의 기본값은 "Root"
            tvModelTree.Nodes.Add("Root");

            // 컨텍스트 메뉴 초기화
            _contextMenu = new ContextMenuStrip();

            List<InspWindowType> windowTypeList;
            windowTypeList = new List<InspWindowType> { InspWindowType.Chip, InspWindowType.lead, InspWindowType.Sub,InspWindowType.ID};
            
            foreach (InspWindowType windowType in windowTypeList)
                _contextMenu.Items.Add(new ToolStripMenuItem(windowType.ToString(), null, AddNode_Click) { Tag = windowType });

            tvModelTree.AfterCheck += tvModelTree_AfterCheck; // ← 추가
        }

        private void tvModelTree_MouseDown(object sender, MouseEventArgs e)
        {
            //Root 노드에서 마우스 오른쪽 버튼 클릭 시에, 팝업 메뉴 생성
            if (e.Button == MouseButtons.Right)
            {
                TreeNode clickedNode = tvModelTree.GetNodeAt(e.X, e.Y);
                if (clickedNode != null && clickedNode.Text == "Root")
                {
                    tvModelTree.SelectedNode = clickedNode;
                    _contextMenu.Show(tvModelTree, e.Location);
                }
            }
        }

        //팝업 메뉴에서, 메뉴 선택시 실행되는 함수
        private void AddNode_Click(object sender, EventArgs e)
        {
            if (tvModelTree.SelectedNode != null & sender is ToolStripMenuItem)
            {
                ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
                InspWindowType windowType = (InspWindowType)menuItem.Tag;
                AddNewROI(windowType);
            }
        }

        //imageViewer에 ROI 추가 기능 실행
        private void AddNewROI(InspWindowType inspWindowType)
        {
            CameraForm cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm != null)
            {
                cameraForm.AddRoi(inspWindowType);
            }
        }

        //현재 모델 전체의 ROI를 트리 모델에 업데이트
        public void UpdateDiagramEntity()
        {
            // 1) 빌드 중 이벤트 폭주 방지
            tvModelTree.AfterCheck -= tvModelTree_AfterCheck;

            tvModelTree.Nodes.Clear();
            TreeNode rootNode = tvModelTree.Nodes.Add("Root");

            Model model = Global.Inst.InspStage.CurModel;
            var windowList = model?.InspWindowList ?? new List<InspWindow>();
            if (windowList.Count > 0)
            {
                foreach (InspWindow window in windowList)
                {
                    if (window == null) continue;

                    // 2) 반드시 Tag에 연결(핵심)
                    string label = $"{window.InspWindowType} [{window.WindowArea.X},{window.WindowArea.Y},{window.WindowArea.Width}x{window.WindowArea.Height}]";
                    TreeNode node = new TreeNode(label)
                    {
                        Tag = window,   // ★ 여기가 핵심
                        Checked = true  // 3) 기본은 '보임'
                    };
                    rootNode.Nodes.Add(node);
                }
            }

            tvModelTree.ExpandAll();

            // 4) 빌드 끝나고 이벤트 다시 연결(핵심)
            tvModelTree.AfterCheck += tvModelTree_AfterCheck;
        }

        private void tvModelTree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // 재귀 방어: 자식 일괄 체크 같은 로직이 있으면 일시 분리 권장
            tvModelTree.AfterCheck -= tvModelTree_AfterCheck;

            var win = e.Node.Tag as InspWindow;
            if (win != null)
            {
                // ImageViewCtrl 인스턴스 가져오기 (폼/싱글톤/DI 등 프로젝트 방식대로)
                var viewer = MainForm.GetDockForm<CameraForm>(); // 예시: 도킹에서 찾기
                if (viewer != null)
                {
                    viewer.SetWindowVisible(win, e.Node.Checked); // ← 보이기/숨기기 토글
                }
            }

            tvModelTree.AfterCheck += tvModelTree_AfterCheck;
        }
    }
}
