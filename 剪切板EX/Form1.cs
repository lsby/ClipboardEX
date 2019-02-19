﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace 剪切板EX
{
    public partial class 主窗口 : Form
    {
        private String 配置文件路径 = "./剪切板EX.exe.config";
        private int 鼠标y坐标;
        private int 鼠标滚轮参数;
        private int 剪切板最大数量;
        private int 按钮高度;
        private int 窗口高度;
        private int 边界阈值 = 5;
        private List<int> 边界值 = new List<int>();

        public 主窗口()
        {
            InitializeComponent();

            设置参数();
            显示();
            设置模板();
            设置定时器();
            设置鼠标钩子();
            设置剪切板钩子();
            设置边界值();

            BeginInvoke(new Action(() =>
            {
                隐藏();
            }));
        }

        private void 设置边界值()
        {
            foreach (var s in Screen.AllScreens)
                边界值.Add(s.Bounds.Y);
        }
        private bool 是否在触发范围内(int y)
        {

            foreach (var 边界 in 边界值)
                if (y < 边界 + 边界阈值 && y >= 边界)
                    return true;
            return false;
        }

        private void 设置参数()
        {
            foreach (XElement item in XDocument.Load(配置文件路径).Root.Element("config").Elements())
            {
                if (item.Name.ToString() == "剪切板最大数量")
                    剪切板最大数量 = int.Parse(item.Value);
                else if (item.Name.ToString() == "按钮高度")
                    按钮高度 = int.Parse(item.Value);
                else if (item.Name.ToString() == "窗口高度")
                    窗口高度 = int.Parse(item.Value);
            }
        }

        private void 设置模板()
        {
            var arr = new List<String>();
            foreach (XElement item in XDocument.Load(配置文件路径).Root.Element("template").Elements())
                arr.Add(item.Value);
            arr.Reverse();
            foreach (var s in arr)
                添加右边按钮(s);
        }

        ~主窗口()
        {
            设置取消剪切板钩子();
        }

        private void 设置取消剪切板钩子()
        {
            RemoveClipboardFormatListener(this.Handle);
        }

        private void 设置剪切板钩子()
        {
            AddClipboardFormatListener(this.Handle);
        }

        private void 设置剪切板内容(String str)
        {
            Clipboard.SetDataObject(str, true);
        }

        void 设置鼠标钩子()
        {
            var hook = new MouseKeyHook();
            hook.OnMouseActivity += 回调_鼠标;
            hook.Start();
        }

        void 设置定时器()
        {
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 100;
            t.Tick += new EventHandler(回调_定时器);
            t.Enabled = true;
        }

        void 显示()
        {
            Visible = true;

            this.TopMost = true;
            Width = Screen.PrimaryScreen.Bounds.Width;
            Height = 窗口高度;
            Location = new Point(0, 0);
            控件_sp.SplitterDistance = this.Width / 2;
        }

        void 隐藏()
        {
            Visible = false;
        }

        List<Button> 左边按钮组 = new List<Button>();
        List<Button> 右边按钮组 = new List<Button>();
        enum 位置 { 左边, 右边 }

        void 添加右边按钮(String str)
        {
            添加按钮(str, 位置.右边);
        }

        void 添加左边按钮(String str)
        {
            添加按钮(str, 位置.左边);
        }

        void 添加按钮(String str, 位置 输入位置)
        {
            FlowLayoutPanel 控件 = 输入位置 == 位置.左边 ? 控件_左边 : 控件_右边;
            List<Button> 按钮组 = 输入位置 == 位置.左边 ? 左边按钮组 : 右边按钮组;

            if (控件.Controls.Count != 0 && str == 控件.Controls[0].Text)
                return;

            var button = new Button();
            button.Text = str;
            button.Width = 控件.Width - 2;
            button.Height = 按钮高度;
            button.Margin = new Padding(0);
            button.Click += 回调_按钮点击;
            button.TextAlign = ContentAlignment.TopLeft;

            按钮组.Insert(0, button);
            while (按钮组.Count > 剪切板最大数量)
                按钮组.RemoveAt(按钮组.Count - 1);

            控件.Controls.Clear();
            for (var i = 0; i < 按钮组.Count; i++)
                控件.Controls.Add(按钮组[i]);
        }

        private void 回调_按钮点击(object sender, EventArgs e)
        {
            设置剪切板内容(((Button)sender).Text);
            隐藏();
        }

        void 回调_定时器(object sender, EventArgs e)
        {
            if (!Bounds.Contains(MousePosition) && MouseButtons == MouseButtons.Left)
                隐藏();
            else if (是否在触发范围内(鼠标y坐标) && 鼠标滚轮参数 < 0)
                显示();
            else if (是否在触发范围内(鼠标y坐标) && 鼠标滚轮参数 > 0)
                隐藏();
        }

        void 回调_鼠标(object sender, MouseEventArgs e)
        {
            鼠标y坐标 = e.Y;
            鼠标滚轮参数 = e.Delta;
        }

        void 回调_剪切板()
        {
            添加左边按钮(Clipboard.GetText());
        }

        //===================================================剪切板监听
        [DllImport("user32.dll")]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private static int WM_CLIPBOARDUPDATE = 0x031D;

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
                回调_剪切板();
            else
                base.DefWndProc(ref m);
        }
    }
}
