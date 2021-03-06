﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hangfire.Dashboard.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    #line 2 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire.States;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class DeletedJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");






            
            #line 6 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
  
    Layout = new LayoutPage("Deleted Jobs");

    int from, perPage;
    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);
    string filterString = Query("filterString");
    string filterMethodString = Query("filterMethodString");
    string startDate = Query("startDate");
    string endDate = Query("endDate");
    string startTime = Query("startTime");
    string endTime = Query("endTime");

    var monitor = Storage.GetMonitoringApi();
    var countParameters = new Dictionary<string, string>()
    {
        { "stateName", DeletedState.StateName },
        { "filterString", filterString },
        { "filterMethodString", filterMethodString },
        { "startDate", startDate },
        { "endDate", endDate },
        { "startTime", startTime },
        { "endTime", endTime }
    };

    var jobCount = monitor.JobCountByStateName(countParameters);
    var pager = new Pager(from, perPage, jobCount)
    {
        JobsFilterText = filterString,
        JobsFilterMethodText = filterMethodString,
        JobsFilterStartDate = startDate,
        JobsFilterEndDate = endDate,
        JobsFilterStartTime = startTime,
        JobsFilterEndTime = endTime
    };

    var jobs = monitor.DeletedJobs(pager);


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 47 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">Deleted" +
" Jobs</h1>\r\n\r\n");


            
            #line 52 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
         if (pager.TotalPageCount == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-info\">\r\n                No deleted jobs found" +
".\r\n            </div>\r\n");


            
            #line 57 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n                    <button class=\"js-jobs-list-command btn bt" +
"n-sm btn-primary\"\r\n                            data-url=\"");


            
            #line 63 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                 Write(Url.To("/jobs/deleted/requeue"));

            
            #line default
            #line hidden
WriteLiteral(@"""
                            data-loading-text=""Enqueueing...""
                            disabled=""disabled"">
                        <span class=""glyphicon glyphicon-repeat""></span>
                        Requeue jobs
                    </button>
");


            
            #line 69 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                     if (EnableSearch == true)
                    {
            
            #line default
            #line hidden
WriteLiteral(" <button data-toggle=\"collapse\" data-target=\"#advanced-search-bar\" class=\"btn btn" +
"-sm btn-success\">Advanced Search</button> ");


            
            #line 70 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                                                                                }

            
            #line default
            #line hidden
WriteLiteral("                </div>\r\n");


            
            #line 72 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                 if (EnableSearch == true)
                {

            
            #line default
            #line hidden
WriteLiteral(@"                    <div id=""advanced-search-bar"" class=""collapse well"">
                        <h4 class=""advanced-search-header"">
                            Advanced Search
                        </h4>
                        <div class=""row"">
                            <div class=""col-md-12"">
                                <div class=""form-group"">
                                    <input type=""text"" value="""" id=""filterValueString"" class=""form-control"" placeholder=""Search..."" />
                                </div>
                                <div class=""form-group"">
");


            
            #line 84 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                      
                                        var currentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                                    

            
            #line default
            #line hidden
WriteLiteral(@"                                    <input id=""filterOnDateTime"" name=""filterOnDateTime"" type=""checkbox"" class=""js-jobs-filterOnDateTime-checked"" />
                                    <label for=""filterOnDateTime"">Filter on date time</label>

                                    <div id=""datetime-filters"" class=""row"">
                                        <div class=""col-xs-6"">
                                            <input type=""text"" value=""");


            
            #line 92 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                 Write(currentDateTime);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"datetimeselector-start form-control\" id=\"startDateTime\" />\r\n            " +
"                            </div>\r\n                                        <div" +
" class=\"col-xs-6\">\r\n                                            <input type=\"tex" +
"t\" value=\"");


            
            #line 95 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                 Write(currentDateTime);

            
            #line default
            #line hidden
WriteLiteral(@""" class=""datetimeselector-end form-control"" id=""endDateTime"" />
                                        </div>
                                    </div>
                                </div>
                                <div class=""form-group"">
                                    <input id=""filterOnMethodName"" name=""filterOnMethodName"" type=""checkbox"" class=""js-jobs-filterOnMethodName-checked"" />
                                    <label for=""filterOnMethodName"">Filter on method name</label>
                                    <input type=""text"" value="""" id=""filterMethodString"" class=""form-control"" placeholder=""Method name..."" />
                                </div>
                            </div>
                        </div>
                        <div class=""row"">
                            <div class=""col-md-12"">
                                <button class=""js-jobs-filter-command btn btn-sm btn-success""
                                        data-url=""");


            
            #line 109 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                             Write(Url.To("/jobs/deleted"));

            
            #line default
            #line hidden
WriteLiteral(@""">
                                    <span class=""glyphicon glyphicon-repeat""></span>
                                    Filter jobs
                                </button>
                            </div>
                        </div>
                    </div>
");


            
            #line 116 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            ");


            
            #line 117 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
       Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral(@"
            <table class=""table"">
                <thead>
                    <tr>
                        <th class=""min-width"">
                            <input type=""checkbox"" class=""js-jobs-list-select-all""/>
                        </th>
                        <th class=""min-width"">Id</th>
                        <th>Job</th>
                        <th class=""align-right"">Deleted</th>
                    </tr>
                    </thead>
                    <tbody>
");


            
            #line 130 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                     foreach (var job in jobs)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <tr class=\"js-jobs-list-row ");


            
            #line 132 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                Write(job.Value != null && !job.Value.InDeletedState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 132 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                                                           Write(job.Value != null && job.Value.InDeletedState && job.Value != null ? "hover" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                            <td>\r\n");


            
            #line 134 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                 if (job.Value != null && job.Value.InDeletedState)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <input type=\"checkbox\" class=\"js-jobs-list-ch" +
"eckbox\" name=\"jobs[]\" value=\"");


            
            #line 136 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                                         Write(job.Key);

            
            #line default
            #line hidden
WriteLiteral("\"/>\r\n");


            
            #line 137 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                            </td>\r\n                            <td class=\"min-wid" +
"th\">\r\n                                ");


            
            #line 140 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                           Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 141 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                 if (job.Value != null && !job.Value.InDeletedState)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <span title=\"Job\'s state has been changed whi" +
"le fetching data.\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 144 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                            </td>\r\n");


            
            #line 146 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                             if (job.Value == null)
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <td colspan=\"2\"><em>Job has expired.</em></td>\r\n");


            
            #line 149 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                            }
                            else
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <td>\r\n                                    ");


            
            #line 153 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                               Write(Html.JobNameLink(job.Key, job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                </td>\r\n");



WriteLiteral("                                <td class=\"align-right\">\r\n");


            
            #line 156 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                     if (job.Value.DeletedAt.HasValue)
                                    {
                                        
            
            #line default
            #line hidden
            
            #line 158 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                         if (RelativeTime == true)
                                        {
                                            
            
            #line default
            #line hidden
            
            #line 160 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                       Write(Html.RelativeTime(job.Value.DeletedAt.Value));

            
            #line default
            #line hidden
            
            #line 160 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                         ;
                                        }
                                        else
                                        {
                                            
            
            #line default
            #line hidden
            
            #line 164 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                       Write(Html.Raw(job.Value.DeletedAt.Value.ToString("dd/MM/yyyy HH:mm")));

            
            #line default
            #line hidden

            
            #line 164 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                                             ;
                                        }
                                    }

            
            #line default
            #line hidden
WriteLiteral("\r\n                                </td>\r\n");


            
            #line 169 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </tr>\r\n");


            
            #line 171 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    </tbody>\r\n                </table>\r\n                ");


            
            #line 174 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
           Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n              </div>\r\n");


            
            #line 176 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>\r\n\r\n");


        }
    }
}
#pragma warning restore 1591
