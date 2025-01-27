﻿using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Zametek.Common.ProjectPlan;
using Zametek.Contract.ProjectPlan;
using Zametek.Utility;

namespace Zametek.ViewModel.ProjectPlan
{
    public class ProjectFileExport
        : IProjectFileExport
    {
        #region Fields

        private static readonly IList<string> s_GeneralColumnTitles = new List<string>
        {
            nameof(ProjectPlanModel.ProjectStart),
            nameof(ProjectPlanModel.ResourceSettings.DefaultUnitCost)
        };

        private static readonly IList<string> s_ActivityColumnTitles = new List<string>
        {
            nameof(ActivityModel.Id),
            nameof(ActivityModel.Name),
            nameof(ActivityModel.TargetResources),
            nameof(ActivityModel.TargetResourceOperator),
            nameof(ActivityModel.AllocatedToResources),
            nameof(ActivityModel.HasNoCost),
            nameof(ActivityModel.Duration),
            nameof(ActivityModel.FreeSlack),
            nameof(ActivityModel.TotalSlack),
            nameof(ActivityModel.EarliestStartTime),
            nameof(ActivityModel.LatestStartTime),
            nameof(ActivityModel.EarliestFinishTime),
            nameof(ActivityModel.LatestFinishTime),
            nameof(ActivityModel.MinimumFreeSlack),
            nameof(ActivityModel.MinimumEarliestStartTime),
            nameof(ActivityModel.MinimumEarliestStartDateTime),
            nameof(ActivityModel.MaximumLatestFinishTime),
            nameof(ActivityModel.MaximumLatestFinishDateTime),
            nameof(ActivityModel.Notes)
        };

        private static readonly IList<string> s_DependentActivityColumnTitles = new List<string>
        {
            nameof(DependentActivityModel.Dependencies),
            nameof(DependentActivityModel.ResourceDependencies)
        };

        private static readonly IList<string> s_ResourceColumnTitles = new List<string>
        {
            nameof(ResourceModel.Id),
            nameof(ResourceModel.Name),
            nameof(ResourceModel.IsExplicitTarget),
            nameof(ResourceModel.InterActivityAllocationType),
            nameof(ResourceModel.UnitCost),
            nameof(ResourceModel.DisplayOrder),
            nameof(ResourceModel.AllocationOrder),
            nameof(ResourceModel.ColorFormat)
        };

        private static readonly IList<string> s_ActivitySeverityColumnTitles = new List<string>
        {
            nameof(ActivitySeverityModel.SlackLimit),
            nameof(ActivitySeverityModel.CriticalityWeight),
            nameof(ActivitySeverityModel.FibonacciWeight),
            nameof(ActivitySeverityModel.ColorFormat)
        };

        private static readonly IList<string> s_TrackingPointColumnTitles = new List<string>
        {
            nameof(TrackingPointModel.Time),
            nameof(TrackingPointModel.ActivityId),
            nameof(TrackingPointModel.ActivityName),
            nameof(TrackingPointModel.Value),
            nameof(TrackingPointModel.ValuePercentage)
        };

        private readonly IDateTimeCalculator m_DateTimeCalculator;

        #endregion

        #region Ctors

        public ProjectFileExport(IDateTimeCalculator dateTimeCalculator)//!!)
        {
            m_DateTimeCalculator = dateTimeCalculator;
        }

        #endregion

        #region Private Methods

        private static void DateFromProjectStart(
            int time,
            ICell cell,//!!,
            ICellStyle dateTimeStyle,//!!,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            if (showDates)
            {
                cell.CellStyle = dateTimeStyle;
                cell.SetCellValue(dateTimeCalculator.AddDays(projectStart, time).DateTime);
            }
            else
            {
                cell.SetCellValue(time);
            }
        }

        private static TypeSwitch<object?> AddDateFromProjectStartCase(
            TypeSwitch<object?> typeSwitch,
            ICell cell,//!!,
            ICellStyle dateTimeStyle,//!!,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            return typeSwitch.Case<int>(time => DateFromProjectStart(time, cell, dateTimeStyle, showDates, projectStart, dateTimeCalculator));
        }

        private static Func<TypeSwitch<object?>, ICell, ICellStyle, TypeSwitch<object?>>? GetActivityAppendDateFromProjectStartCaseFunc(
            string columnTitle,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            Func<TypeSwitch<object?>, ICell, ICellStyle, TypeSwitch<object?>>? output = null;

            TypeSwitch<object?> appendCaseCheck(TypeSwitch<object?> x, ICell y, ICellStyle z) =>
                AddDateFromProjectStartCase(x, y, z, showDates, projectStart, dateTimeCalculator);

            columnTitle.ValueSwitchOn()
                .Case(nameof(ActivityModel.EarliestStartTime), _ => output = appendCaseCheck)
                .Case(nameof(ActivityModel.LatestStartTime), _ => output = appendCaseCheck)
                .Case(nameof(ActivityModel.EarliestFinishTime), _ => output = appendCaseCheck)
                .Case(nameof(ActivityModel.LatestFinishTime), _ => output = appendCaseCheck);

            return output;
        }

        private static Func<TypeSwitch<object?>, ICell, ICellStyle, TypeSwitch<object?>>? GetTrackingPointAppendDateFromProjectStartCaseFunc(
            string columnTitle,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            Func<TypeSwitch<object?>, ICell, ICellStyle, TypeSwitch<object?>>? output = null;

            TypeSwitch<object?> appendCaseCheck(TypeSwitch<object?> x, ICell y, ICellStyle z) =>
                AddDateFromProjectStartCase(x, y, z, showDates, projectStart, dateTimeCalculator);

            columnTitle.ValueSwitchOn()
                .Case(nameof(TrackingPointModel.Time), _ => output = appendCaseCheck);

            return output;
        }

        private static void AddToCell(
            object? content,
            ICell cell,//!!,
            ICellStyle dateTimeStyle,//!!,
            Func<TypeSwitch<object?>, ICell, ICellStyle, TypeSwitch<object?>>? appendCaseCheck = null)
        {
            TypeSwitch<object?> typeSwitch = content.TypeSwitchOn();

            if (appendCaseCheck is not null)
            {
                typeSwitch = appendCaseCheck(typeSwitch, cell, dateTimeStyle);
            }

            typeSwitch
                .Case<string>(x => cell.SetCellValue(x))
                .Case<int>(x => cell.SetCellValue(x))
                .Case<double>(x => cell.SetCellValue(x))
                .Case<bool>(x => cell.SetCellValue(x))
                .Case<ColorFormatModel>(x =>
                {
                    cell.SetCellValue(ColorHelper.ColorFormatToHtmlHexCode(x));
                })
                .Case<DateTime>(x =>
                {
                    cell.CellStyle = dateTimeStyle;
                    cell.SetCellValue(x);
                })
                .Case<DateTimeOffset>(x =>
                {
                    cell.CellStyle = dateTimeStyle;
                    cell.SetCellValue(x.DateTime);
                })
                .Case<IEnumerable>(x =>
                {
                    cell.SetCellValue(string.Join(DependenciesStringValidationRule.Separator, x.Cast<object>().Select(y => y.ToString())));
                })
                .Default(x => cell.SetCellValue(x?.ToString()));
        }

        private static void WriteGeneralToWorkbook(
            ProjectPlanModel projectPlan,//!!,
            IWorkbook workbook,//!!,
            ICellStyle titleStyle)//!!)
        {
            ICellStyle dateTimeCellStyle = workbook.CreateCellStyle();
            dateTimeCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ISheet sheet = workbook.CreateSheet(Resource.ProjectPlan.Reporting.Reporting_WorksheetGeneral);

            int rowIndex = 0;

            {
                int titleColumnIndex = 0;
                IRow titleRow = sheet.CreateRow(rowIndex);

                foreach (string columnTitle in s_GeneralColumnTitles)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(columnTitle, cell, dateTimeCellStyle);
                    titleColumnIndex++;
                }

                rowIndex++;
            }
            {
                Type activityType = typeof(ProjectPlanModel);
                PropertyInfo[] propertyInfos = activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                var propertyInfoLookup = propertyInfos.ToDictionary(x => x.Name);

                IRow row = sheet.CreateRow(rowIndex);
                int columnIndex = 0;

                foreach (string columnTitle in s_GeneralColumnTitles)
                {
                    ICell cell = row.CreateCell(columnIndex);

                    columnTitle.ValueSwitchOn()
                        .Case(nameof(ProjectPlanModel.ProjectStart),
                            colName => AddToCell(projectPlan.ProjectStart, cell, dateTimeCellStyle))
                        .Case(nameof(ProjectPlanModel.ResourceSettings.DefaultUnitCost),
                            colName => AddToCell(projectPlan.ResourceSettings.DefaultUnitCost, cell, dateTimeCellStyle));

                    columnIndex++;
                }

                rowIndex++;
            }
            {
                int titleColumnIndex = 0;

                foreach (string columnTitle in s_GeneralColumnTitles)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
            }
        }

        private static void WriteActivitiesToWorkbook(
            IEnumerable<DependentActivityModel> dependentActivities,//!!,
            IWorkbook workbook,//!!,
            ICellStyle titleStyle,//!!,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            ICellStyle dateTimeCellStyle = workbook.CreateCellStyle();
            dateTimeCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ISheet sheet = workbook.CreateSheet(Resource.ProjectPlan.Reporting.Reporting_WorksheetActivities);

            int rowIndex = 0;

            {
                int titleColumnIndex = 0;
                IRow titleRow = sheet.CreateRow(rowIndex);

                foreach (string columnTitle in s_ActivityColumnTitles)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(columnTitle, cell, dateTimeCellStyle);
                    titleColumnIndex++;
                }
                foreach (string columnTitle in s_DependentActivityColumnTitles)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(columnTitle, cell, dateTimeCellStyle);
                    titleColumnIndex++;
                }

                rowIndex++;
            }
            {
                Type activityType = typeof(ActivityModel);
                PropertyInfo[] activityPropertyInfos = activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                var activityPropertyInfoLookup = activityPropertyInfos.ToDictionary(x => x.Name);

                Type dependentActivityType = typeof(DependentActivityModel);
                PropertyInfo[] dependentActivityPropertyInfos = dependentActivityType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                var dependentActivityPropertyInfoLookup = dependentActivityPropertyInfos.ToDictionary(x => x.Name);

                foreach (DependentActivityModel dependentActivity in dependentActivities)
                {
                    IRow row = sheet.CreateRow(rowIndex);
                    int columnIndex = 0;
                    ActivityModel activity = dependentActivity.Activity;

                    foreach (string columnTitle in s_ActivityColumnTitles)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        PropertyInfo? propertyInfo = activityPropertyInfoLookup[columnTitle];

                        if (propertyInfo is not null)
                        {
                            object? content = propertyInfo.GetValue(activity);
                            AddToCell(
                                content,
                                cell,
                                dateTimeCellStyle,
                                GetActivityAppendDateFromProjectStartCaseFunc(columnTitle, showDates, projectStart, dateTimeCalculator));
                        }
                        columnIndex++;
                    }
                    foreach (string columnTitle in s_DependentActivityColumnTitles)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        PropertyInfo? propertyInfo = dependentActivityPropertyInfoLookup[columnTitle];

                        if (propertyInfo is not null)
                        {
                            object? content = propertyInfo.GetValue(dependentActivity);
                            AddToCell(content, cell, dateTimeCellStyle);
                        }
                        columnIndex++;
                    }

                    rowIndex++;
                }
            }
            {
                int titleColumnIndex = 0;

                foreach (string columnTitle in s_ActivityColumnTitles)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
                foreach (string columnTitle in s_DependentActivityColumnTitles)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
            }
        }

        private static void WriteItemsToWorkbook<T>(
            IEnumerable<T> items,//!!,
            IEnumerable<string> columnTitles,//!!,
            string sheetTitle,
            IWorkbook workbook,//!!,
            ICellStyle titleStyle)//!!)
        {
            ICellStyle dateTimeCellStyle = workbook.CreateCellStyle();
            dateTimeCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ISheet sheet = workbook.CreateSheet(sheetTitle);

            int rowIndex = 0;

            {
                int titleColumnIndex = 0;
                IRow titleRow = sheet.CreateRow(rowIndex);

                foreach (string columnTitle in columnTitles)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(columnTitle, cell, dateTimeCellStyle);
                    titleColumnIndex++;
                }

                rowIndex++;
            }
            {
                Type itemType = typeof(T);
                PropertyInfo[] propertyInfos = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                var propertyInfoLookup = propertyInfos.ToDictionary(x => x.Name);

                foreach (T item in items)
                {
                    IRow row = sheet.CreateRow(rowIndex);
                    int columnIndex = 0;

                    foreach (string columnTitle in columnTitles)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        PropertyInfo? propertyInfo = propertyInfoLookup[columnTitle];

                        if (propertyInfo is not null)
                        {
                            object? content = propertyInfo.GetValue(item);
                            AddToCell(content, cell, dateTimeCellStyle);
                        }
                        columnIndex++;
                    }

                    rowIndex++;
                }
            }
            {
                int titleColumnIndex = 0;

                foreach (string columnTitle in columnTitles)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
            }
        }

        private static void WriteTrackersToWorkbook<T>(
            IEnumerable<ActivityModel> activities,//!!,
            Func<TrackerModel, T> trackerFunc,//!!,
            string sheetTitle,
            IWorkbook workbook,//!!,
            ICellStyle titleStyle,//!!,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            ICellStyle dateTimeCellStyle = workbook.CreateCellStyle();
            dateTimeCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ICellStyle dateTimeTitleCellStyle = workbook.CreateCellStyle();
            dateTimeTitleCellStyle.CloneStyleFrom(titleStyle);
            dateTimeTitleCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ISheet sheet = workbook.CreateSheet(sheetTitle);

            int rowIndex = 0;
            Debug.Assert(activities.Select(x => x.Trackers.Count).Distinct().Count() <= 1);
            int trackerCount = activities.Select(x => x.Trackers.Count).FirstOrDefault();

            {
                int titleColumnIndex = 0;
                TypeSwitch<object?> appendCaseCheck(TypeSwitch<object?> x, ICell y, ICellStyle z) =>
                    AddDateFromProjectStartCase(x, y, z, showDates, projectStart, dateTimeCalculator);

                IRow titleRow = sheet.CreateRow(rowIndex);

                // Title row (Activity ID column).
                ICell iDCell = titleRow.CreateCell(titleColumnIndex);
                iDCell.CellStyle = titleStyle;
                AddToCell(nameof(ActivityModel.Id), iDCell, dateTimeTitleCellStyle);
                titleColumnIndex++;

                for (int i = 0; i < trackerCount; i++)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(i, cell, dateTimeTitleCellStyle, appendCaseCheck);
                    titleColumnIndex++;
                }
                rowIndex++;
            }
            {
                // Activity Trackers.
                foreach (ActivityModel activity in activities)
                {
                    IRow row = sheet.CreateRow(rowIndex);
                    int columnIndex = 0;

                    // Activity Id.
                    ICell iDCell = row.CreateCell(columnIndex);
                    iDCell.CellStyle = titleStyle;
                    AddToCell(activity.Id, iDCell, dateTimeCellStyle);
                    columnIndex++;

                    // Tracker values.
                    for (int i = 0; i < trackerCount; i++)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        AddToCell(trackerFunc(activity.Trackers[i]), cell, dateTimeCellStyle);
                        columnIndex++;
                    }

                    rowIndex++;
                }
            }
            {
                // Resize columns.
                int titleColumnIndex = 0;
                sheet.AutoSizeColumn(titleColumnIndex);
                titleColumnIndex++;

                for (int i = 0; i < trackerCount; i++)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
            }
        }

        private static void WriteResourceChartToWorkbook<T>(
            ResourceSeriesSetModel resourceSeriesSet,//!!,
            Func<ResourceSeriesModel, int, T> resourceseriesFunc,//!!,
            string sheetTitle,
            IWorkbook workbook,//!!,
            ICellStyle titleStyle,//!!,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            ICellStyle dateTimeCellStyle = workbook.CreateCellStyle();
            dateTimeCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ISheet sheet = workbook.CreateSheet(sheetTitle);

            IEnumerable<ResourceSeriesModel> combinedResourceSeries = resourceSeriesSet.Combined.OrderBy(x => x.DisplayOrder);

            int rowIndex = 0;

            Debug.Assert(combinedResourceSeries.Select(x => x.ResourceSchedule.ActivityAllocation.Count).Distinct().Count() <= 1);
            int valueCount = combinedResourceSeries.Select(x => x.ResourceSchedule.ActivityAllocation.Count).FirstOrDefault();

            {
                int titleColumnIndex = 0;
                IRow titleRow = sheet.CreateRow(rowIndex);

                ICell timeCell = titleRow.CreateCell(titleColumnIndex);
                timeCell.CellStyle = titleStyle;
                AddToCell(Resource.ProjectPlan.Reporting.Reporting_Time, timeCell, dateTimeCellStyle);
                titleColumnIndex++;

                foreach (ResourceSeriesModel resourceSeries in combinedResourceSeries)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(resourceSeries.Title, cell, dateTimeCellStyle);
                    titleColumnIndex++;
                }

                rowIndex++;
            }
            {
                // Pivot the series values.
                for (int timeIndex = 0; timeIndex < valueCount; timeIndex++)
                {
                    IRow row = sheet.CreateRow(rowIndex);
                    int columnIndex = 0;

                    // Time.
                    ICell timeCell = row.CreateCell(columnIndex);
                    timeCell.CellStyle = titleStyle;
                    DateFromProjectStart(timeIndex, timeCell, dateTimeCellStyle, showDates, projectStart, dateTimeCalculator);
                    columnIndex++;

                    // Values.
                    foreach (ResourceSeriesModel resourceSeries in combinedResourceSeries)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        AddToCell(
                            resourceseriesFunc(resourceSeries, timeIndex),
                            cell,
                            dateTimeCellStyle);
                        columnIndex++;
                    }

                    rowIndex++;
                }
            }
            {
                // Resize columns.
                int titleColumnIndex = 0;
                sheet.AutoSizeColumn(titleColumnIndex);
                titleColumnIndex++;

                foreach (ResourceSeriesModel resourceSeries in combinedResourceSeries)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
            }
        }

        private static void WriteEarnedValueChartToWorkbook(
            IEnumerable<TrackingPointModel> trackingPoints,//!!,
            string sheetTitle,
            IWorkbook workbook,//!!,
            ICellStyle titleStyle,//!!,
            bool showDates,
            DateTimeOffset projectStart,
            IDateTimeCalculator dateTimeCalculator)//!!)
        {
            ICellStyle dateTimeCellStyle = workbook.CreateCellStyle();
            dateTimeCellStyle.DataFormat = workbook.GetCreationHelper().CreateDataFormat().GetFormat(DateTimeCalculator.DateFormat);

            ISheet sheet = workbook.CreateSheet(sheetTitle);

            int rowIndex = 0;

            {
                int titleColumnIndex = 0;
                IRow titleRow = sheet.CreateRow(rowIndex);

                foreach (string columnTitle in s_TrackingPointColumnTitles)
                {
                    ICell cell = titleRow.CreateCell(titleColumnIndex);
                    cell.CellStyle = titleStyle;
                    AddToCell(columnTitle, cell, dateTimeCellStyle);
                    titleColumnIndex++;
                }

                rowIndex++;
            }
            {
                Type activityType = typeof(TrackingPointModel);
                PropertyInfo[] propertyInfos = activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                var propertyInfoLookup = propertyInfos.ToDictionary(x => x.Name);

                foreach (TrackingPointModel trackingPoint in trackingPoints)
                {
                    IRow row = sheet.CreateRow(rowIndex);
                    int columnIndex = 0;

                    foreach (string columnTitle in s_TrackingPointColumnTitles)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        PropertyInfo? propertyInfo = propertyInfoLookup[columnTitle];

                        if (propertyInfo is not null)
                        {
                            object? content = propertyInfo.GetValue(trackingPoint);
                            AddToCell(
                                content,
                                cell,
                                dateTimeCellStyle,
                                GetTrackingPointAppendDateFromProjectStartCaseFunc(columnTitle, showDates, projectStart, dateTimeCalculator));
                        }
                        columnIndex++;
                    }

                    rowIndex++;
                }
            }
            {
                // Resize columns.
                int titleColumnIndex = 0;

                foreach (string columnTitle in s_TrackingPointColumnTitles)
                {
                    sheet.AutoSizeColumn(titleColumnIndex);
                    titleColumnIndex++;
                }
            }
        }

        #endregion

        #region IProjectFileExport Members

        public void ExportProjectPlanFile(
            ProjectPlanModel projectPlan,
            ResourceSeriesSetModel resourceSeriesSet,
            TrackingSeriesSetModel trackingSeriesSet,
            bool showDates,
            string filename)
        {
            IWorkbook workbook = new XSSFWorkbook();
            IFont titleFont = workbook.CreateFont();

            titleFont.IsBold = true;
            titleFont.FontHeightInPoints = 12;
            //titleFont.Underline = FontUnderlineType.Single;

            ICellStyle titleStyle = workbook.CreateCellStyle();
            titleStyle.SetFont(titleFont);

            WriteGeneralToWorkbook(
                projectPlan,
                workbook,
                titleStyle);

            WriteActivitiesToWorkbook(
                projectPlan.DependentActivities,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteItemsToWorkbook(
                projectPlan.ResourceSettings.Resources,
                s_ResourceColumnTitles,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetResources,
                workbook,
                titleStyle);

            WriteItemsToWorkbook(
                projectPlan.ArrowGraphSettings.ActivitySeverities,
                s_ActivitySeverityColumnTitles,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetActivitySeverities,
                workbook,
                titleStyle);

            WriteTrackersToWorkbook(
                projectPlan.DependentActivities.Select(x => x.Activity),
                tracker => tracker.PercentageComplete,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetTrackerPercentageComplete,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteTrackersToWorkbook(
                projectPlan.DependentActivities.Select(x => x.Activity),
                tracker => tracker.IsIncluded,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetTrackerDaysIncluded,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteResourceChartToWorkbook(
                resourceSeriesSet,
                (resourceSeries, timeIndex) =>
                {
                    return resourceSeries.ResourceSchedule.ActivityAllocation[timeIndex];
                },
                Resource.ProjectPlan.Reporting.Reporting_WorksheetResourceChartData,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteResourceChartToWorkbook(
                resourceSeriesSet,
                (resourceSeries, timeIndex) =>
                {
                    return resourceSeries.ResourceSchedule.ActivityAllocation[timeIndex] ? resourceSeries.UnitCost : 0.0;
                },
                Resource.ProjectPlan.Reporting.Reporting_WorksheetResourceChartCosts,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteEarnedValueChartToWorkbook(
                trackingSeriesSet.Plan,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetEarnedValueChartPlan,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteEarnedValueChartToWorkbook(
                trackingSeriesSet.Effort,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetEarnedValueChartEffort,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            WriteEarnedValueChartToWorkbook(
                trackingSeriesSet.Progress,
                Resource.ProjectPlan.Reporting.Reporting_WorksheetEarnedValueChartProgress,
                workbook,
                titleStyle,
                showDates,
                projectPlan.ProjectStart,
                m_DateTimeCalculator);

            using var stream = File.OpenWrite(filename);
            workbook.Write(stream);
        }

        public async Task ExportProjectPlanFileAsync(
            ProjectPlanModel projectPlan,
            ResourceSeriesSetModel resourceSeriesSet,
            TrackingSeriesSetModel trackingSeriesSet,
            bool showDates,
            string filename)
        {
            await Task.Run(() => ExportProjectPlanFile(projectPlan, resourceSeriesSet, trackingSeriesSet, showDates, filename));
        }

        #endregion
    }
}
