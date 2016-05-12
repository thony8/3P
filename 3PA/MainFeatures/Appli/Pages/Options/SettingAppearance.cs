﻿#region header
// ========================================================================
// Copyright (c) 2016 - Julien Caillon (julien.caillon@gmail.com)
// This file (SettingAppearance.cs) is part of 3P.
// 
// 3P is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// 3P is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with 3P. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Drawing;
using System.Linq;
using YamuiFramework.Controls;

namespace _3PA.MainFeatures.Appli.Pages.Options {
    internal partial class SettingAppearance : YamuiPage {

        private static YamuiColorRadioButton _checkButton;

        public SettingAppearance() {
            InitializeComponent();

            // AccentColors picker
            int x = 0;
            int y = 0;
            foreach (var accentColor in ThemeManager.GetAccentColors) {
                var newColorPicker = new YamuiColorRadioButton();
                PanelAccentColor.Controls.Add(newColorPicker);
                newColorPicker.CheckedChanged += NewColorPickerOnCheckedChanged;
                newColorPicker.BackColor = accentColor;
                newColorPicker.Location = new Point(x, y);
                newColorPicker.Size = new Size(50, 50);
                //newColorPicker.Bounds = new Rectangle(x, y, 50, 50);
                if (y + 2*newColorPicker.Height > PanelAccentColor.Height) {
                    x += newColorPicker.Width;
                    y = 0;
                } else
                    y += newColorPicker.Height;
                if (ThemeManager.Current.AccentColor == accentColor) {
                    _checkButton = newColorPicker;
                    newColorPicker.Checked = true;
                }
                toolTip.SetToolTip(newColorPicker, "Click me to set a new accent color for the current theme");
            }

            // toggle
            tg_colorOn.CheckedChanged += TgOnCheckedChanged;
            tg_colorOn.Checked = Config.Instance.GlobalDontUseSyntaxHighlightTheme;

            tg_override.CheckedChanged += TgOnCheckedChanged;
            tg_override.Checked = Config.Instance.GlobalOverrideNppTheme;
            UpdateToggle();

            // tooltips
            toolTip.SetToolTip(cbApplication, "Choose the theme you wish to use for the software");
            toolTip.SetToolTip(cbSyntax, "Choose the theme you wish to use for the syntax highlighting");
            toolTip.SetToolTip(tg_colorOn, "Toggle this option on if you are using your own User Defined Language<br><br>By default, 3P created a new UDL called 'OpenEdgeABL' and applies the selected theme below<br>each time the user switches the current document<br>By toggling this on, you will prevent this behavior and you can define your own UDL<br><br><i>If you toggle this, select the UDL to use from the Language menu before you can see any changes</i>");
            toolTip.SetToolTip(tg_override, "Toggle this option on if don't want 3P to override certain colors of Notepad++<br>like the selection / caret line color for instance<br>In that case, you will continue using the style settings of Notepad++ and 3P<br>will only control the colors of the language itself.<br><br><i>You need to restart Notepad++ to see any changes</i>");

            linkurl.Text = @"<img src='Help'><a href='" + Config.UrlHelpCustomThemes + @"'>How to customize the look of 3P?</a>";
        }

        public override void OnShow() {
            // themes combo box
            cbApplication.SelectedIndexChanged -= CbApplicationOnSelectedIndexChanged;
            cbApplication.DataSource = ThemeManager.GetThemesList().Select(theme => theme.ThemeName).ToList();
            cbApplication.SelectedIndex = Config.Instance.ThemeId;
            cbApplication.SelectedIndexChanged += CbApplicationOnSelectedIndexChanged;

            // syntax combo
            cbSyntax.SelectedIndexChanged -= CbSyntaxSelectedIndexChanged;
            cbSyntax.DataSource = Style.GetThemesList().Select(theme => theme.Name).ToList();
            cbSyntax.SelectedIndex = Config.Instance.SyntaxHighlightThemeId;
            cbSyntax.SelectedIndexChanged += CbSyntaxSelectedIndexChanged;
        }

        private void TgOnCheckedChanged(object sender, EventArgs eventArgs) {
            Config.Instance.GlobalDontUseSyntaxHighlightTheme = tg_colorOn.Checked;
            Config.Instance.GlobalOverrideNppTheme = tg_override.Checked;
            UpdateToggle();
        }

        private void UpdateToggle() {
            if (tg_colorOn.Checked) {
                tg_colorOn.Text = "Use a custom User Defined Language";
                cbSyntax.Hide();
            } else {
                tg_colorOn.Text = "Use the themes provided by 3P, select one below : ";
                cbSyntax.Show();
            }
            if (tg_override.Checked) {
                tg_override.Text = "Let 3P override notepad++ themes (for instance, replace selection color)";
            } else {
                tg_override.Text = "Use the theme of 3P strictly for the syntax highlighting";
            }
        }

        /// <summary>
        /// Changing theme
        /// </summary>
        private void CbApplicationOnSelectedIndexChanged(object sender, EventArgs eventArgs) {
            try {
                ThemeManager.Current = ThemeManager.GetThemesList()[cbApplication.SelectedIndex];
                ThemeManager.Current.AccentColor = ThemeManager.Current.ThemeAccentColor;
                Config.Instance.AccentColor = ThemeManager.Current.AccentColor;
                _checkButton.Checked = false;
            } catch (Exception x) {
                if (!(x is NullReferenceException))
                    ErrorHandler.Log(x.Message);
            } finally {
                Config.Instance.ThemeId = cbApplication.SelectedIndex;
                ThemeManager.PlsRefresh();
            }
            
        }

        /// <summary>
        /// Changing syntax theme
        /// </summary>
        private void CbSyntaxSelectedIndexChanged(object sender, EventArgs eventArgs) {
            try {
                Style.CurrentTheme = Style.GetThemesList()[cbSyntax.SelectedIndex];
            } catch (Exception x) {
                ErrorHandler.Log(x.Message);
            } finally {
                Config.Instance.SyntaxHighlightThemeId = cbSyntax.SelectedIndex;
                if (Plug.IsCurrentFileProgress)
                    Style.SetSyntaxStyles();
            }
        }

        /// <summary>
        /// Changing accent Color
        /// </summary>
        private void NewColorPickerOnCheckedChanged(object sender, EventArgs eventArgs) {
            YamuiColorRadioButton rb = sender as YamuiColorRadioButton;
            if (rb != null && rb.Checked) {
                ThemeManager.Current.AccentColor = rb.BackColor;
                Config.Instance.AccentColor = ThemeManager.Current.AccentColor;
                _checkButton = rb;
                ThemeManager.PlsRefresh();
            }
        }
    }
}
