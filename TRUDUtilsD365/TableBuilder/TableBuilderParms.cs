﻿using System;
using System.Collections.Generic;
using Microsoft.Dynamics.AX.Metadata.Core.MetaModel;
using TRUDUtilsD365.Kernel;
using Microsoft.Dynamics.AX.Metadata.MetaModel;
using Microsoft.Dynamics.Framework.Tools.MetaModel.Core;
using TRUDUtilsD365.AddTableFindMethod;
using AxTableField = Microsoft.Dynamics.AX.Metadata.MetaModel.AxTableField;

namespace TRUDUtilsD365.TableBuilder
{
    public class TableBuilderParms
    {
        public Boolean IsCreateTable { get; set; }
        public string TableName { get; set; } = "";
        public string TableLabel { get; set; } = "";
        public string TableVarName { get; set; } = "";

        public Boolean IsCreateForm { get; set; }
        public string FormName { get; set; } = "";
        public string FormLabel { get; set; } = "";
        public string FormHelp { get; set; } = "";

        public string PrimaryKeyEdtName { get; set; } = "";
        public string KeyFieldName { get; set; } = "Id";

        private AxHelper _axHelper;

        private string _logString;

        void AddLog(string logLocal)
        {
            _logString += logLocal;
        }

        public void DisplayLog()
        {
            CoreUtility.DisplayInfo($"The following elements({_logString}) were created and added to the project");
        }

        public void CreateTable()
        {
            _logString = "";
            if (_axHelper == null)
            {
                _axHelper = new AxHelper();
            }

            if (IsCreateTable)
            {
                DoEdtCreate();
                DoTableCreate();
            }

            if (IsCreateForm)
            {
                DoFormCreate();
                DoMenuItemCreate();
            }
        }

        void DoEdtCreate()
        {
            AxEdt newEdt = _axHelper.MetadataProvider.Edts.Read(PrimaryKeyEdtName);
            if (newEdt != null)
            {
                return;
            }
            //need to create a EDT
            newEdt = new AxEdtString();
            newEdt.Name = PrimaryKeyEdtName;
            newEdt.Extends ="SysGroup" ;

            _axHelper.MetaModelService.CreateExtendedDataType(newEdt, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(newEdt);

            AddLog($"EDT: {newEdt.Name}; ");

        }

        void DoMenuItemCreate()
        {
            AxMenuItemDisplay axMenuItemDisplay =_axHelper.MetadataProvider.MenuItemDisplays.Read(FormName);
            if (axMenuItemDisplay != null)
            {
                return;                
            }

            axMenuItemDisplay = new AxMenuItemDisplay { Name = FormName, Object = FormName, Label = FormLabel, HelpText = FormHelp };
            _axHelper.MetaModelService.CreateMenuItemDisplay(axMenuItemDisplay, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(axMenuItemDisplay);

            AddLog($"MenuItem: {axMenuItemDisplay.Name}; ");
        }

        void DoFormCreate()
        {
            AxForm newForm = _axHelper.MetadataProvider.Forms.Read(FormName);
            if (newForm != null)
            {
                return;
            }
            newForm = new AxForm();
            newForm.Name = FormName;

            AxMethod axMethod = new AxMethod();
            axMethod.Name = "classDeclaration";
            axMethod.Source = $"[Form]{Environment.NewLine}public class {newForm.Name} extends FormRun " +
                              Environment.NewLine + "{" + Environment.NewLine + "}";
            newForm.AddMethod(axMethod);

            string dsName = TableName;

            AxFormDataSourceRoot axFormDataSource = new AxFormDataSourceRoot();
            axFormDataSource.Name = dsName;
            axFormDataSource.Table = TableName;
            axFormDataSource.InsertIfEmpty = NoYes.No;
            newForm.AddDataSource(axFormDataSource);

            //newForm.Design.Pattern = "SimpleList"; add apply pattern
            //newForm.Design.PatternVersion = "1.1";
            newForm.Design.Caption = FormLabel;

            newForm.Design.AddControl(new AxFormActionPaneControl { Name = "MainActionPane" });

            var filterGrp = new AxFormGroupControl{Name = "FilterGroup",Pattern = "CustomAndQuickFilters", PatternVersion = "1.1"};

            AxFormControlExtension quickFilterControl = new AxFormControlExtension {Name = "QuickFilterControl"};
            AxFormControlExtensionProperty formControlExtensionProperty = new AxFormControlExtensionProperty();
            formControlExtensionProperty.Name = "targetControlName";
            formControlExtensionProperty.Type = CompilerBaseType.String;
            formControlExtensionProperty.Value = "MainGrid";
            quickFilterControl.ExtensionProperties.Add(formControlExtensionProperty);

            filterGrp.AddControl(new AxFormControl{Name = "QuickFilter",FormControlExtension = quickFilterControl});
            newForm.Design.AddControl(filterGrp);
            AxFormGridControl axFormGridControl = new AxFormGridControl {Name = "MainGrid", DataSource = dsName};

            AxFormGroupControl overviewGroupControl = new AxFormGroupControl
            {
                Name = "Overview", DataGroup = "Overview", DataSource = dsName
            };

            axFormGridControl.AddControl(overviewGroupControl);
            newForm.Design.AddControl(axFormGridControl);

            _axHelper.MetaModelService.CreateForm(newForm, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(newForm);

            AddLog($"Form: {newForm.Name}; ");
        }


        void DoTableCreate()
        {
            AxTable newTable = _axHelper.MetadataProvider.Tables.Read(TableName);
            if (newTable == null)
            {
                newTable                  = new AxTable();
                newTable.Name             = TableName;
                newTable.Label            = TableLabel;
                newTable.TitleField1      = KeyFieldName;
                newTable.CacheLookup      = RecordCacheLevel.Found;
                newTable.ClusteredIndex   = $"{KeyFieldName}Idx";
                newTable.PrimaryIndex     = newTable.ClusteredIndex;
                newTable.ReplacementKey   = newTable.ClusteredIndex;
                newTable.TableGroup       = TableGroup.Group;
                newTable.CreatedBy        = NoYes.Yes;
                newTable.CreatedDateTime  = NoYes.Yes;
                newTable.ModifiedBy       = NoYes.Yes;
                newTable.ModifiedDateTime = NoYes.Yes;

                AxTableField primaryField = new AxTableFieldString();
                primaryField.Name              = KeyFieldName;
                primaryField.ExtendedDataType  = PrimaryKeyEdtName;
                primaryField.IgnoreEDTRelation = NoYes.Yes;
                primaryField.AllowEdit         = NoYes.No;
                primaryField.Mandatory         = NoYes.Yes;
                newTable.AddField(primaryField);

                AxTableField descriptionField = new AxTableFieldString();
                descriptionField.Name              = "Description";
                descriptionField.ExtendedDataType  = "Description";
                newTable.AddField(descriptionField);

                AxTableIndexField axTableIndexField = new AxTableIndexField();
                axTableIndexField.DataField = KeyFieldName;
                axTableIndexField.Name      = KeyFieldName;
                AxTableIndex axTableIndex = new AxTableIndex();
                axTableIndex.Name         = newTable.ClusteredIndex;
                axTableIndex.AlternateKey = NoYes.Yes;
                axTableIndex.AddField(axTableIndexField);
                newTable.AddIndex(axTableIndex);

                AxTableFieldGroup axTableFieldGroup;
                AxTableFieldGroupField axTableFieldGroupField;

                axTableFieldGroup = new AxTableFieldGroup { Name = "AutoReport", IsSystemGenerated = NoYes.Yes};
                axTableFieldGroupField = new AxTableFieldGroupField
                {
                    Name      = KeyFieldName,
                    DataField = KeyFieldName
                };
                axTableFieldGroup.AddField(axTableFieldGroupField);
                newTable.AddFieldGroup(axTableFieldGroup);

                axTableFieldGroup = new AxTableFieldGroup { Name = "AutoLookup", IsSystemGenerated = NoYes.Yes };
                newTable.AddFieldGroup(axTableFieldGroup);

                axTableFieldGroup = new AxTableFieldGroup { Name = "AutoIdentification", IsSystemGenerated = NoYes.Yes, AutoPopulate = NoYes.Yes};
                newTable.AddFieldGroup(axTableFieldGroup);

                axTableFieldGroup = new AxTableFieldGroup { Name = "AutoSummary", IsSystemGenerated = NoYes.Yes };
                newTable.AddFieldGroup(axTableFieldGroup);

                axTableFieldGroup = new AxTableFieldGroup { Name = "AutoBrowse", IsSystemGenerated = NoYes.Yes };
                newTable.AddFieldGroup(axTableFieldGroup);

                axTableFieldGroup = new AxTableFieldGroup {Name = "Overview", Label = "Overview"};
                axTableFieldGroupField = new AxTableFieldGroupField
                {
                    Name = KeyFieldName, DataField = KeyFieldName
                };
                axTableFieldGroup.AddField(axTableFieldGroupField);
                if (descriptionField != null)
                {
                    axTableFieldGroupField = new AxTableFieldGroupField
                    {
                        Name      = descriptionField.Name,
                        DataField = descriptionField.Name
                    };
                    axTableFieldGroup.AddField(axTableFieldGroupField);
                }
                newTable.AddFieldGroup(axTableFieldGroup);

                AddTableFindMethodParms findMethodParms = new AddTableFindMethodParms();
                findMethodParms.IsCreateFind = true;
                findMethodParms.IsTestMode   = true;
                findMethodParms.TableName    = TableName;
                findMethodParms.VarName      = TableVarName;
                findMethodParms.Fields = new List<AxTableFieldParm>
                {
                    new AxTableFieldParm
                    {
                        FieldName = KeyFieldName, FieldType = PrimaryKeyEdtName, IsMandatory = true
                    }
                };

                AxMethod axMethod = new AxMethod();
                axMethod.Name     = "find";
                axMethod.IsStatic = true;
                axMethod.Source   = findMethodParms.GenerateResult();
                newTable.AddMethod(axMethod);

                _axHelper.MetaModelService.CreateTable(newTable, _axHelper.ModelSaveInfo);
                _axHelper.AppendToActiveProject(newTable);

                AddLog($"Table: {newTable.Name}; ");

                AxEdt edtLocal = _axHelper.MetadataProvider.Edts.Read(PrimaryKeyEdtName);
                if (edtLocal != null)
                {
                    edtLocal.ReferenceTable = TableName;
                    edtLocal.AddTableReference(TableName, KeyFieldName);

                    _axHelper.MetaModelService.UpdateExtendedDataType(edtLocal, _axHelper.ModelSaveInfo);
                    _axHelper.AppendToActiveProject(edtLocal);

                    //AddLog($"EDT: {edtLocal.Name}; ");
                }
            }
        }

    }
}