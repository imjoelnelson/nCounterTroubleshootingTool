﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TS_General_QCmodule.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0,1,99,2,3,4,5,6")]
        public string includedAnnotCols {
            get {
                return ((string)(this["includedAnnotCols"]));
            }
            set {
                this["includedAnnotCols"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0,1,2,3,4,5,6,7,8,9,10,11")]
        public string includedHeaderRows {
            get {
                return ((string)(this["includedHeaderRows"]));
            }
            set {
                this["includedHeaderRows"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0,1,2,99,3,4,5,6,7,8,9,10")]
        public string includedDspAnnotCols {
            get {
                return ((string)(this["includedDspAnnotCols"]));
            }
            set {
                this["includedDspAnnotCols"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0,1,2,3,4,5,6,7,8,9,10,11")]
        public string includedDspHeaderRows {
            get {
                return ((string)(this["includedDspHeaderRows"]));
            }
            set {
                this["includedDspHeaderRows"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool dspSortByPlexRow {
            get {
                return ((bool)(this["dspSortByPlexRow"]));
            }
            set {
                this["dspSortByPlexRow"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool includeFlagTable {
            get {
                return ((bool)(this["includeFlagTable"]));
            }
            set {
                this["includeFlagTable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool dspIncludeFlagTable {
            get {
                return ((bool)(this["dspIncludeFlagTable"]));
            }
            set {
                this["dspIncludeFlagTable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ligBkgSubtract {
            get {
                return ((bool)(this["ligBkgSubtract"]));
            }
            set {
                this["ligBkgSubtract"] = value;
            }
        }
    }
}
