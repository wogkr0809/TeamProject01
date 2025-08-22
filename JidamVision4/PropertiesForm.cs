using JidamVision4.Algorithm;
using JidamVision4.Core;
using JidamVision4.Property;
using JidamVision4.Teach;
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

namespace JidamVision4
{
    //#2_DOCKPANEL#4 PropertiesForm 클래스 는 도킹 가능하도록 상속을 변경

    //public partial class PropertiesForm: Form
    public partial class PropertiesForm : DockContent
    {
        //#3_CAMERAVIEW_PROPERTY#4 속성탭을 관리하기 위한 딕셔너리
        Dictionary<string, TabPage> _allTabs = new Dictionary<string, TabPage>();

        private SurfaceDefectProp surfProp;
        private TabPage _surfaceTab, _binaryTab;
        BinaryProp _binaryProp;

        public PropertiesForm()
        {
            InitializeComponent();
        }

        //#3_CAMERAVIEW_PROPERTY#6 속성탭이 있다면 그것을 반환하고, 없다면 생성
        private void LoadOptionControl(InspectType inspType)
        {
            if (inspType == InspectType.InspSurfaceDefect) return;

            string tabName = inspType.ToString();

            // 이미 있는 TabPage인지 확인
            foreach (TabPage tabPage in tabPropControl.TabPages)
            {
                if (tabPage.Text == tabName)
                    return;
            }

            // 딕셔너리에 있으면 추가
            if (_allTabs.TryGetValue(tabName, out TabPage page))
            {
                tabPropControl.TabPages.Add(page);
                return;
            }

            // 새로운 UserControl 생성
            var _inspProp = CreateUserControl(inspType);
            if (_inspProp == null) return;

            var newTab = new TabPage(tabName) { Dock = DockStyle.Fill };
            _inspProp.Dock = DockStyle.Fill;
            newTab.Controls.Add(_inspProp);
            tabPropControl.TabPages.Add(newTab);
            tabPropControl.SelectedTab = newTab;

            _allTabs[tabName] = newTab;
        }

        //#11_MODEL_TREE#2 PropertyType을 InspectType으로 변경

        //#3_CAMERAVIEW_PROPERTY# 5 속성 탭을 생성하는 메서드
        private UserControl CreateUserControl(InspectType inspPropType)
        {
            UserControl curProp = null;
            switch (inspPropType)
            {
                case InspectType.InspBinary:
                    BinaryProp blobProp = new BinaryProp();

                    //#7_BINARY_PREVIEW#8 이진화 속성 변경시 발생하는 이벤트 추가
                    blobProp.RangeChanged += RangeSlider_RangeChanged;

                    //#18_IMAGE_CHANNEL#13 이미지 채널 변경시 이벤트 추가
                    blobProp.ImageChannelChanged += ImageChannelChanged;
                    curProp = blobProp;
                    break;
                //#11_MATCHING#5 패턴매칭 속성창 추가
                case InspectType.InspMatch:
                    MatchInspProp matchProp = new MatchInspProp();
                    curProp = matchProp;
                    break;
                case InspectType.InspFilter:
                    ImageFilterProp filterProp = new ImageFilterProp();
                    curProp = filterProp;
                    break;
                case InspectType.InspAIModule:
                    AIModuleProp aiModuleProp = new AIModuleProp();
                    curProp = aiModuleProp;
                    break;
                case InspectType.InspSurfaceDefect:
                    surfProp = new SurfaceDefectProp();
                    curProp = surfProp;
                    break;
                default:
                    MessageBox.Show("유효하지 않은 옵션입니다.");
                    return null;
            }
            return curProp;
        }

        //#11_MODEL_TREE#3 InspWindow에서 사용하는 알고리즘을 모두 탭에 추가
        public void ShowProperty(InspWindow window)
        {
            foreach (InspAlgorithm algo in window.AlgorithmList)
            {
                LoadOptionControl(algo.InspectType);
            }
            EnsureSurfaceTab(window);
        }

        public void ResetProperty()
        {
            tabPropControl.TabPages.Clear();
            _allTabs.Clear();
            _surfaceTab = null;
            surfProp = null;
        }

        public void UpdateProperty(InspWindow window)
        {
            if (window == null)
                return;

            EnsureSurfaceTab(window);

            foreach (TabPage tabPage in tabPropControl.TabPages)
            {
                if (tabPage.Controls.Count == 0) continue;

                var uc = tabPage.Controls[0] as UserControl;


                if (uc is BinaryProp binaryProp)
                {
                    var blobAlgo = window.FindInspAlgorithm(InspectType.InspBinary) as BlobAlgorithm;
                    binaryProp.SetAlgorithm(blobAlgo);
                }
                else if (uc is MatchInspProp matchProp)
                {
                    var matchAlgo = window.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;
                    if (matchAlgo != null)
                    {
                        window.PatternLearn();
                        matchProp.SetAlgorithm(matchAlgo);
                    }
                }
            }
            if (surfProp != null)
            {
                var surAlgo = window.AlgorithmList
                                        .FirstOrDefault(a => a is SurfaceDefectAlgorithm)
                                        as SurfaceDefectAlgorithm;
                surfProp.Bind(window, surAlgo);
            }

            if (_binaryProp != null && window != null)
            {
                var blob = GetOrAddBlob(window);
                _binaryProp.SetAlgorithm(blob);

            }

        }

        //#7_BINARY_PREVIEW#7 이진화 속성 변경시 발생하는 이벤트 구현
        private void RangeSlider_RangeChanged(object sender, RangeChangedEventArgs e)
        {
            // 속성값을 이용하여 이진화 임계값 설정
            int lowerValue = e.LowerValue;
            int upperValue = e.UpperValue;
            bool invert = e.Invert;
            ShowBinaryMode showBinMode = e.ShowBinMode;
            Global.Inst.InspStage.PreView?.SetBinary(lowerValue, upperValue, invert, showBinMode);
        }

        //#18_IMAGE_CHANNEL#14 이미지 채널 변경시 프리뷰에 이미지 채널 설정
        private void ImageChannelChanged(object sender, ImageChannelEventArgs e)
        {
            Global.Inst.InspStage.SetPreviewImage(e.Channel);
        }

        private void EnsureSurfaceTab(InspWindow window)
        {
            bool needSurface = window != null &&
                window.AlgorithmList != null &&
                window.AlgorithmList.Any(a => a is SurfaceDefectAlgorithm);

            if (needSurface)
            {
                if (_surfaceTab == null)
                {
                    _surfaceTab = new TabPage("Surface");
                    surfProp = new SurfaceDefectProp { Dock = DockStyle.Fill };
                    _surfaceTab.Controls.Add(surfProp);
                    tabPropControl.TabPages.Add(_surfaceTab);
                }
                if (_binaryTab == null)
                {
                    _binaryProp = new BinaryProp { Dock = DockStyle.Fill };

                    _binaryProp.RangeChanged += RangeSlider_RangeChanged;
                    _binaryProp.ImageChannelChanged += ImageChannelChanged;

                    _binaryTab = new TabPage("InspBinary") { Name = "TabBinary" };
                    _binaryTab.Controls.Add(_binaryProp);
                    tabPropControl.TabPages.Add(_binaryTab);
                }

                var blob = GetOrAddBlob(window);
                _binaryProp.SetAlgorithm(blob);

            }
            else
            {
                if (_surfaceTab != null)
                {
                    tabPropControl.TabPages.Remove(_surfaceTab);
                    _surfaceTab.Dispose();
                    _surfaceTab = null;
                    surfProp = null;
                }
            }
        }
        private static BlobAlgorithm GetOrAddBlob(InspWindow w)
        {
            var blob = w.FindInspAlgorithm(InspectType.InspBinary) as BlobAlgorithm;
            if (blob == null)
            {
                blob = new BlobAlgorithm();
                w.AlgorithmList.Add(blob);
            }
            return blob;
        }
    }
}
