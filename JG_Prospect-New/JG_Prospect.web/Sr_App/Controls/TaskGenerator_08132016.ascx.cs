﻿#region "-- using -"

using JG_Prospect.BLL;
using JG_Prospect.Common;
using JG_Prospect.Common.modal;
using Saplin.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Net.Mail;
using System.Globalization;
using System.Configuration;
using Ionic.Zip;
using JG_Prospect.App_Code;
using System.Web.Services;
using Newtonsoft.Json;
using System.Linq;

#endregion

namespace JG_Prospect.Sr_App.Controls
{
    public partial class TaskGenerator : System.Web.UI.UserControl
    {
        #region "--Properties--"

        string strSubtaskSeq = "sbtaskseq";

        /// <summary>
        /// Set control view mode.
        /// </summary>
        public bool IsAdminMode
        {
            get
            {
                bool returnVal = false;

                if (ViewState["AMODE"] != null)
                {
                    returnVal = Convert.ToBoolean(ViewState["AMODE"]);
                }

                return returnVal;
            }
            set
            {
                ViewState["AMODE"] = value;
            }

        }

        public String LastSubTaskSequence
        {
            get
            {
                String val = string.Empty;

                if (ViewState[strSubtaskSeq] != null && !string.IsNullOrEmpty(ViewState[strSubtaskSeq].ToString()))
                {
                    val = ViewState[strSubtaskSeq].ToString();
                }
                return val;
            }

            set
            {

                ViewState[strSubtaskSeq] = value;

            }

        }


        #endregion

        #region "--Page methods--"

        protected void Page_Load(object sender, EventArgs e)
        {
            ScriptManager scriptManager = ScriptManager.GetCurrent(this.Page);
            scriptManager.RegisterPostBackControl(this.gdTaskUsers);

            if (!IsPostBack)
            {
                this.IsAdminMode = CommonFunction.CheckAdminMode();

                // Add mode for task control.
                controlMode.Value = "0";
                SetControlDisplay();
                LoadFilters();
                SearchTasks(null);
                LoadPopupDropdown();
            }
        }

        #endregion

        #region "--Control Events--"

        #region Filters - Search Task

        protected void ddlDesignation_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFilterUsersByDesgination();
            SearchTasks(null);
        }

        protected void ddlUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            SearchTasks(null);
        }

        protected void ddlTaskStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            SearchTasks(null);
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchTasks(null);
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            SearchTasks(null);
        }

        #endregion

        protected void lbtnAddNew_Click(object sender, EventArgs e)
        {
            clearAllFormData();
            SetSubTaskSectionView(false);

            SetTaskView();

            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "open popup", "EditTask(0,\"Add New Task\");", true);
        }

        protected void btnLoadMore_Click(object sender, EventArgs e)
        {
            SearchTasks(50);
            ScriptManager.RegisterStartupScript((sender as Control), this.GetType(), "expand", "SetHeaderSectionHeight();", true);
        }

        #region gvTasks - Task List

        protected void gvTasks_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            //if (!this.IsAdminMode)
            //{
            //    e.Row.Cells[e.Row.Cells.Count - 1].Visible = false;
            //}

            if (e.Row.RowType == DataControlRowType.DataRow)
            {

                DropDownList ddlgvTaskStatus = (DropDownList)e.Row.FindControl("ddlgvTaskStatus");

                if (ddlgvTaskStatus != null)
                {
                    ddlgvTaskStatus.SelectedValue = DataBinder.Eval(e.Row.DataItem, "Status").ToString();
                }

                if (Convert.ToBoolean(DataBinder.Eval(e.Row.DataItem, "IsDeleted")))
                {
                    e.Row.CssClass = "ui-state-default dark-gray-background";
                }

            }
        }

        protected void gvTasks_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditTask")
            {
                clearAllFormData();
                SetSubTaskSectionView(false);

                controlMode.Value = "1";
                hdnTaskId.Value = e.CommandArgument.ToString();
                LoadTaskData(e.CommandArgument.ToString(), true);
            }
            //else if (e.CommandName == "RemoveTask")
            //{
            //    DeletaTask(e.CommandArgument.ToString());
            //}
        }

        #endregion

        #region gdTaskUsers - Task History

        protected void gdTaskUsers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {


                if (String.IsNullOrEmpty(DataBinder.Eval(e.Row.DataItem, "attachments").ToString()))
                {
                    LinkButton lbtnAttachment = (LinkButton)e.Row.FindControl("lbtnAttachment");
                    lbtnAttachment.Visible = false;
                }

                Label lblStatus = (Label)e.Row.FindControl("lblStatus");

                int TaskStatus = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "Status"));
                String statusString = String.Empty;

                switch (TaskStatus)
                {
                    case 1:
                        statusString = "Open";
                        break;
                    case 2:
                        statusString = "Assigned";
                        break;
                    case 3:
                        statusString = "In Progress";
                        break;
                    case 4:
                        statusString = "Pending";
                        break;
                    case 5:
                        statusString = "Re-Opened";
                        break;
                    case 6:
                        statusString = "Closed";
                        break;
                }

                lblStatus.Text = statusString;
            }
        }

        protected void gdTaskUsers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DownLoadFiles")
            {
                // Allow download only if files are attached.
                if (!String.IsNullOrEmpty(e.CommandArgument.ToString()))
                {
                    DownloadUserAttachments(e.CommandArgument.ToString());
                }
            }

        }

        #endregion

        #region Popup - Add / Edit Task

        protected void gvSubTasks_RowDataBound(object sender, GridViewRowEventArgs e)
        {

        }

        protected void lbtnAddNewSubTask_Click(object sender, EventArgs e)
        {
            ClearSubTaskData();
            string[] subtaskListIDSuggestion = CommonFunction.getSubtaskSequencing(this.LastSubTaskSequence);
            if (subtaskListIDSuggestion.Length > 0)
            {
                if (subtaskListIDSuggestion.Length > 1)
                {
                    listIDOpt1.Text = subtaskListIDSuggestion[0];
                    listIDOpt2.Text = subtaskListIDSuggestion[1];
                }
                else
                {
                    listIDOpt1.Text = subtaskListIDSuggestion[0];
                }

            }
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "slid down sub task", "$('#" + divSubTask.ClientID + "').slideDown('slow');", true);
        }

        protected void btnSaveSubTask_Click(object sender, EventArgs e)
        {
            SaveSubTask();
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "slid up sub task", "$('#" + divSubTask.ClientID + "').slideUp('slow');", true);
        }

        protected void btnSaveTask_Click(object sender, EventArgs e)
        {
            //Save task master details
            SaveTask();

            // Save assgined designation.
            SaveTaskDesignations();

            //Save details of users to whom task is assgined.
            SaveAssignedTaskUsers();

            if (controlMode.Value == "0")
            {
                ScriptManager.RegisterStartupScript(this.Page, GetType(), "closepopup", "CloseTaskPopup();", true);
            }
            else
            {
                CommonFunction.ShowAlertFromUpdatePanel(this.Page, "Task updated successfully!");
            }

            SearchTasks(null);
        }

        protected void btnRemoveTask_Click(object sender, EventArgs e)
        {
            DeletaTask(hdnDeleteTaskId.Value);
            ScriptManager.RegisterStartupScript((sender as Control), this.GetType(), "HidePopup", "CloseTaskPopup();", true);
        }

        protected void btnAddNote_Click(object sender, EventArgs e)
        {
            SaveTaskNotesNAttachments();
            hdnAttachments.Value = "";
        }

        protected void ddlUserDesignation_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadUsersByDesgination();

            ddcbAssigned_SelectedIndexChanged(sender, e);

            ddlUserDesignation.Texts.SelectBoxCaption = "Select";

            foreach (ListItem item in ddlUserDesignation.Items)
            {
                if (item.Selected)
                {
                    ddlUserDesignation.Texts.SelectBoxCaption = item.Text;
                    break;
                }
            }
        }

        protected void ddcbAssigned_SelectedIndexChanged(object sender, EventArgs e)
        {
            #region 'Commented as Not needed'
            /*
            if (controlMode.Value == "0")
            {
                DataSet dsUsers = new DataSet();
                DataSet tempDs;
                List<string> SelectedUsersID = new List<string>();
                List<string> SelectedUsers = new List<string>();
                foreach (System.Web.UI.WebControls.ListItem item in ddcbAssigned.Items)
                {
                    if (item.Selected)
                    {
                        SelectedUsersID.Add(item.Value);
                        SelectedUsers.Add(item.Text);
                        tempDs = TaskGeneratorBLL.Instance.GetInstallUserDetails(Convert.ToInt32(item.Value));
                        dsUsers.Merge(tempDs);
                    }
                }
                if (dsUsers.Tables.Count != 0)
                {
                    gdTaskUsers.DataSource = dsUsers;
                    gdTaskUsers.DataBind();
                }
                else
                {
                    gdTaskUsers.DataSource = null;
                    gdTaskUsers.DataBind();
                }
            } 
            */
            #endregion

            ddcbAssigned.Texts.SelectBoxCaption = "--Open--";

            foreach (ListItem item in ddcbAssigned.Items)
            {
                if (item.Selected)
                {
                    ddcbAssigned.Texts.SelectBoxCaption = item.Text;
                    break;
                }
            }
        }

        protected void ddlgvTaskStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddlGvTaskStatus = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddlGvTaskStatus.NamingContainer;
            HiddenField hdnTaskId = (HiddenField)row.FindControl("hdnTaskId");

            Int32 taskId = Convert.ToInt32(hdnTaskId.Value);

            UInt16 taskStatus = Convert.ToUInt16(ddlGvTaskStatus.SelectedItem.Value);

            UpdateTaskStatus(taskId, taskStatus);
        }

        #endregion

        #endregion

        #region "--Private Methods--"

        private void SetControlDisplay()
        {
            if (!this.IsAdminMode)
            {
                tdAdd.Visible = false;
                tdAddCap.Visible = false;
                tdDesig.Visible = false;
                tdDesigCap.Visible = false;
                tdUserCap.Visible = false;
                tdUsers.Visible = false;
            }
        }

        /// <summary>
        /// Load filter dropdowns for task
        /// </summary>
        private void LoadFilters()
        {
            DataSet dsFilters = TaskGeneratorBLL.Instance.GetAllUsersNDesignationsForFilter();

            if (dsFilters != null && dsFilters.Tables.Count > 0)
            {
                DataTable dtUsers = dsFilters.Tables[0];
                //DataTable dtDesignations = dsFilters.Tables[1];

                ddlUsers.DataSource = dtUsers;
                // ddlDesignation.DataSource = dtDesignations;

                ddlUsers.DataTextField = "FirstName";
                ddlUsers.DataValueField = "Id";
                ddlUsers.DataBind();

                //ddlDesignation.DataTextField = "Designation";
                //ddlDesignation.DataValueField = "Designation";
                //ddlDesignation.DataBind();

                ddlUsers.Items.Insert(0, new ListItem("--All--", "0"));
                ddlDesignation.Items.Insert(0, new ListItem("--All--", "0"));
                HighlightInterviewUsers(dtUsers, null, ddlUsers);
            }
        }

        /// <summary>
        /// Search tasks with parameters choosen by user.
        /// </summary>
        private void SearchTasks(int? RecordstoPull)
        {

            int? UserID = null;
            string Title = String.Empty, Designation = String.Empty, Designations = String.Empty, Statuses = String.Empty;
            Int16? Status = null;
            DateTime? CreatedFrom = null, CreatedTo = null;


            // this is for paging based data fetch, in header view case it will be always page numnber 0 and page size 5
            int Start = 0, PageLimit = 5;

            if (RecordstoPull != null)
            {
                PageLimit = Convert.ToInt32(RecordstoPull);
            }

            PrepareSearchFilters(ref UserID, ref Title, ref Designation, ref Status, ref CreatedFrom, ref CreatedTo, ref Statuses, ref Designations);

            DataSet dsFilters = TaskGeneratorBLL.Instance.GetTasksList(UserID, Title, Designation, Status, CreatedFrom, CreatedTo, Statuses, Designations, this.IsAdminMode, Start, PageLimit);

            if (dsFilters != null && dsFilters.Tables.Count > 0)
            {
                gvTasks.DataSource = dsFilters;
                gvTasks.DataBind();
            }

        }

        /// <summary>
        /// To load Designation to popup dropdown
        /// </summary>
        private void LoadPopupDropdown()
        {
            //DataSet dsdesign = TaskGeneratorBLL.Instance.GetInstallUsers(1, "");
            //DataSet ds = TaskGeneratorBLL.Instance.GetTaskUserDetails(1);
            //ddlUserDesignation.DataSource = dsdesign;
            //ddlUserDesignation.DataTextField = "Designation";
            //ddlUserDesignation.DataValueField = "Designation";
            //ddlUserDesignation.DataBind();
        }

        private void DeletaTask(string TaskId)
        {
            TaskGeneratorBLL.Instance.DeleteTask(Convert.ToUInt64(TaskId));
            hdnDeleteTaskId.Value = String.Empty;
            SearchTasks(null);
        }

        private void doSearch(object sender, EventArgs e)
        {

        }

        private void UpdateTaskStatus(Int32 taskId, UInt16 Status)
        {
            Task task = new Task();
            task.TaskId = taskId;
            task.Status = Status;
            
            int result = TaskGeneratorBLL.Instance.UpdateTaskStatus(task);    // save task master details

            SearchTasks(null);

            String AlertMsg;

            if (result > 0)
            {
                AlertMsg = "Status changed successfully!";

            }
            else
            {
                AlertMsg = "Status change was not successfull, Please try again later.";
            }

            CommonFunction.ShowAlertFromUpdatePanel(this.Page, AlertMsg);

        }

        private string GetInstallIdFromDesignation(string designame)
        {
            string prefix = "";
            switch (designame)
            {
                case "Admin":
                    prefix = "ADM";
                    break;
                case "Jr. Sales":
                    prefix = "JSL";
                    break;
                case "Jr Project Manager":
                    prefix = "JPM";
                    break;
                case "Office Manager":
                    prefix = "OFM";
                    break;
                case "Recruiter":
                    prefix = "REC";
                    break;
                case "Sales Manager":
                    prefix = "SLM";
                    break;
                case "Sr. Sales":
                    prefix = "SSL";
                    break;
                case "IT - Network Admin":
                    prefix = "ITNA";
                    break;
                case "IT - Jr .Net Developer":
                    prefix = "ITJN";
                    break;
                case "IT - Sr .Net Developer":
                    prefix = "ITSN";
                    break;
                case "IT - Android Developer":
                    prefix = "ITAD";
                    break;
                case "IT - PHP Developer":
                    prefix = "ITPH";
                    break;
                case "IT - SEO / BackLinking":
                    prefix = "ITSB";
                    break;
                case "Installer - Helper":
                    prefix = "INH";
                    break;
                case "Installer – Journeyman":
                    prefix = "INJ";
                    break;
                case "Installer – Mechanic":
                    prefix = "INM";
                    break;
                case "Installer - Lead mechanic":
                    prefix = "INLM";
                    break;
                case "Installer – Foreman":
                    prefix = "INF";
                    break;
                case "Commercial Only":
                    prefix = "COM";
                    break;
                case "SubContractor":
                    prefix = "SBC";
                    break;
                default:
                    prefix = "TSK";
                    break;
            }
            return prefix;
        }

        private void LoadUsersByDesgination()
        {
            DataSet dsUsers;

            // DropDownCheckBoxes ddlAssign = (FindControl("ddcbAssigned") as DropDownCheckBoxes);
            // DropDownList ddlDesignation = (DropDownList)sender;

            string designations = GetSelectedDesignationsString();

            dsUsers = TaskGeneratorBLL.Instance.GetInstallUsers(2, designations);

            ddcbAssigned.Items.Clear();
            ddcbAssigned.DataSource = dsUsers;
            ddcbAssigned.DataTextField = "FristName";
            ddcbAssigned.DataValueField = "Id";
            ddcbAssigned.DataBind();

            HighlightInterviewUsers(dsUsers.Tables[0], ddcbAssigned, null);
        }

        private void HighlightInterviewUsers(DataTable dtUsers, DropDownCheckBoxes ddlUsers, DropDownList ddlFilterUsers)
        {
            if (dtUsers.Rows.Count > 0)
            {
                var rows = dtUsers.AsEnumerable();

                //get all users comma seperated ids with interviewdate status
                String InterviewDateUsers = String.Join(",", (from r in rows where (r.Field<string>("Status") == "InterviewDate" || r.Field<string>("Status") == "Interview Date") select r.Field<Int32>("Id").ToString()));

                // for each userid find it into user dropdown list and apply red color to it.
                foreach (String user in InterviewDateUsers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ListItem item;

                    if (ddlUsers != null)
                    {
                        item = ddlUsers.Items.FindByValue(user);
                    }
                    else
                    {
                        item = ddlFilterUsers.Items.FindByValue(user);
                    }

                    if (item != null)
                    {
                        item.Attributes.Add("style", "color:red;");
                    }
                }

            }
        }

        private string GetSelectedDesignationsString()
        {
            String returnVal = string.Empty;
            StringBuilder sbDesignations = new StringBuilder();

            foreach (ListItem item in ddlUserDesignation.Items)
            {
                if (item.Selected)
                {
                    sbDesignations.Append(String.Concat(item.Text, ","));
                }
            }

            if (sbDesignations.Length > 0)
            {
                returnVal = sbDesignations.ToString().Substring(0, sbDesignations.ToString().Length - 1);
            }

            return returnVal;
        }

        private void LoadFilterUsersByDesgination()
        {
            DataSet dsUsers;

            // DropDownCheckBoxes ddlAssign = (FindControl("ddcbAssigned") as DropDownCheckBoxes);
            // DropDownList ddlDesignation = (DropDownList)sender;
            string designation = ddlDesignation.SelectedValue;

            dsUsers = TaskGeneratorBLL.Instance.GetInstallUsers(2, designation);

            ddlUsers.DataSource = dsUsers;
            ddlUsers.DataTextField = "FristName";
            ddlUsers.DataValueField = "Id";
            ddlUsers.DataBind();

            ddlUsers.Items.Insert(0, new ListItem("--All--", "0"));

        }

        /// <summary>
        /// Prepare search filters choosen by users before performing search
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="Title"></param>
        /// <param name="Designation"></param>
        /// <param name="Status"></param>
        /// <param name="CreatedOn"></param>
        private void PrepareSearchFilters(ref int? UserID, ref string Title, ref string Designation, ref short? Status, ref DateTime? CreatedFrom, ref DateTime? CreatedTo, ref string Statuses, ref string Designations)
        {

            if (this.IsAdminMode)
            {
                if (ddlUsers.SelectedIndex > 0)
                {
                    UserID = Convert.ToInt32(ddlUsers.SelectedItem.Value);
                }

                if (ddlDesignation.SelectedIndex > 0)
                {
                    Designation = ddlDesignation.SelectedItem.Value;
                }


            }
            else
            {
                UserID = Convert.ToInt32(Session[JG_Prospect.Common.SessionKey.Key.UserId.ToString()]);

                Designations = GetUserDepartmentAllDesignations(Session["DesigNew"].ToString());

                //search all status for now, later if requirement changed can remove status accordingly.
                Statuses = "1,2,3,4,5,6";
            }

            if (!String.IsNullOrEmpty(txtSearch.Text))
            {
                Title = txtSearch.Text;
            }
            if (ddlTaskStatus.SelectedIndex > 0)
            {
                Status = Convert.ToInt16(ddlTaskStatus.SelectedItem.Value);
            }

            if (!String.IsNullOrEmpty(txtFromDate.Text) && !String.IsNullOrEmpty(txtToDate.Text))
            {
                CreatedFrom = Convert.ToDateTime(txtFromDate.Text);
                CreatedTo = Convert.ToDateTime(txtToDate.Text);
            }

        }

        /// <summary>
        /// Get all designations from department to which user's designation belongs to.
        /// Ex. if user has designation IT - Network Admin , here all IT related task will be listed.
        /// </summary>
        /// <param name="UserDesignation"></param>
        /// <returns></returns>
        private string GetUserDepartmentAllDesignations(string UserDesignation)
        {
            string returnString = string.Empty;
            const string ITDesignations = "IT - Network Admin,IT - Jr .Net Developer,IT - Sr .Net Developer,IT - Android Developer,IT - PHP Developer,IT - SEO / BackLinking";

            if (UserDesignation.Contains("IT"))
            {
                returnString = ITDesignations;
            }
            else
            {
                returnString = UserDesignation;
            }

            return returnString;
        }

        /// <summary>
        /// To clear the popup details after save
        /// </summary>
        private void clearAllFormData()
        {
            txtTaskTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            ddlUserDesignation.ClearSelection();
            ddlUserDesignation.Texts.SelectBoxCaption = "Select";
            ddcbAssigned.Items.Clear();
            ddcbAssigned.Texts.SelectBoxCaption = "--Open--";
            cmbStatus.ClearSelection();
            ddlUserAcceptance.ClearSelection();
            txtDueDate.Text = string.Empty;
            txtHours.Text = string.Empty;
            gdTaskUsers.DataSource = null;
            gdTaskUsers.DataBind();
            txtNote.Text = string.Empty;
            hdnTaskId.Value = "0";
            controlMode.Value = "0";
        }

        private void ClearSubTaskData()
        {
            hdnSubTaskId.Value = "0";
            txtSubTaskTitle.Text =
            txtSubTaskDescription.Text =
            txtSubTaskDueDate.Text =
            txtSubTaskHours.Text = string.Empty;
            upAddSubTask.Update();
        }

        /// <summary>
        /// Save task master details, user information and user attachments.
        /// Created By: Yogesh Keraliya
        /// </summary>
        private void SaveTask()
        {
            int userId = Convert.ToInt16(Session[JG_Prospect.Common.SessionKey.Key.UserId.ToString()]);
            Task task = new Task();
            task.TaskId = Convert.ToInt32(hdnTaskId.Value);
            task.Title = Server.HtmlEncode(txtTaskTitle.Text);
            task.Description = Server.HtmlEncode(txtDescription.Text);
            task.Status = Convert.ToUInt16(cmbStatus.SelectedItem.Value);
            task.DueDate = txtDueDate.Text;
            task.Hours = txtHours.Text;
            task.CreatedBy = userId;
            task.Mode = Convert.ToInt32(controlMode.Value);
            task.InstallId = GetInstallIdFromDesignation(ddlUserDesignation.SelectedItem.Text);

            Int64 ItaskId = TaskGeneratorBLL.Instance.SaveOrDeleteTask(task);    // save task master details

            if (controlMode.Value == "0")
            {
                hdnTaskId.Value = ItaskId.ToString();
            }
        }

        private void SaveSubTask()
        {
            int userId = Convert.ToInt16(Session[JG_Prospect.Common.SessionKey.Key.UserId.ToString()]);
            Task task = new Task();
            task.TaskId = Convert.ToInt32(hdnSubTaskId.Value);
            task.Title = txtSubTaskTitle.Text;
            task.Description = txtSubTaskDescription.Text;
            task.Status = 1; // 1 : Open
            task.DueDate = txtSubTaskDueDate.Text;
            task.Hours = txtSubTaskHours.Text;
            task.CreatedBy = userId;
            task.Mode = 0;// Convert.ToInt32(controlMode.Value);
            task.InstallId = GetInstallIdFromDesignation(ddlUserDesignation.SelectedItem.Text);
            task.ParentTaskId = Convert.ToInt32(hdnTaskId.Value);
            Int64 ItaskId = TaskGeneratorBLL.Instance.SaveOrDeleteTask(task);    // save task master details

            hdnSubTaskId.Value = ItaskId.ToString();

            SetSubTaskDetails(TaskGeneratorBLL.Instance.GetSubTasks(Convert.ToInt32(hdnTaskId.Value)).Tables[0]);
        }

        private void SaveTaskDesignations()
        {
            //if task id is available to save its note and attachement.
            if (hdnTaskId.Value != "0")
            {
                String designations = GetSelectedDesignationsString();
                if (!string.IsNullOrEmpty(designations))
                {
                    int indexofComma = designations.IndexOf(',');
                    int copyTill = indexofComma > 0 ? indexofComma : designations.Length;

                    string designationcode = GetInstallIdFromDesignation(designations.Substring(0, copyTill));

                    TaskGeneratorBLL.Instance.SaveTaskDesignations(Convert.ToUInt64(hdnTaskId.Value), designations, designationcode);
                }
            }
        }

        /// <summary>
        /// Save user's to whom task is assigned. 
        /// </summary>
        private void SaveAssignedTaskUsers()
        {
            //if task id is available to save its note and attachement.
            if (hdnTaskId.Value != "0")
            {
                string strUsersIds = string.Empty;

                foreach (ListItem item in ddcbAssigned.Items)
                {
                    if (item.Selected)
                    {
                        strUsersIds = strUsersIds + (item.Value + ",");
                    }
                }

                // removes any extra comma "," from the end of the string.
                strUsersIds = strUsersIds.TrimEnd(',');

                // save (insert / delete) assigned users.
                bool isSuccessful = TaskGeneratorBLL.Instance.SaveTaskAssignedUsers(Convert.ToUInt64(hdnTaskId.Value), strUsersIds);

                // send email to selected users.
                if (strUsersIds.Length > 0)
                {
                    if (isSuccessful)
                    {
                        // Change task status to assigned = 2.
                        if (cmbStatus.SelectedIndex > 0 && cmbStatus.SelectedItem.Value == "1")
                        {
                            UpdateTaskStatus(Convert.ToInt32(hdnTaskId.Value), 2); 
                        }

                        SendEmailToAssignedUsers(strUsersIds);
                    }
                }
                // send email to all users of the department as task is assigned to designation, but not to any specific user.
                else
                {
                    string strUserIDs = "";

                    foreach (ListItem item in ddcbAssigned.Items)
                    {
                        strUserIDs += string.Concat(item.Value, ",");
                    }

                    SendEmailToAssignedUsers(strUserIDs.TrimEnd(','));
                }
            }
        }

        ///// <summary>
        ///// Save user's to whom task is assigned. 
        ///// </summary>
        //private void SaveAssignedTaskUsers()
        //{
        //    //if task id is available to save its note and attachement.
        //    if (hdnTaskId.Value != "0")
        //    {
        //        Boolean? isCreatorUser = null;

        //        foreach (ListItem item in ddcbAssigned.Items)
        //        {
        //            if (item.Selected)
        //            {
        //                // Save task notes and user information, returns TaskUpdateId for reference to add in user attachments.
        //                Int32 TaskUpdateId = SaveTaskNote(Convert.ToInt64(hdnTaskId.Value), isCreatorUser, Convert.ToInt32(item.Value), item.Text);
        //            }

        //        }


        //    }

        //}

        /// <summary>
        /// Save task note and attachment added by user.
        /// </summary>
        private void SaveTaskNotesNAttachments()
        {
            //if task id is available to save its note and attachement.
            if (hdnTaskId.Value != "0")
            {
                Boolean? isCreatorUser = null;

                //if it is task is created than control mode will be 0 and Admin user has created task.
                if (controlMode.Value == "0")
                {
                    isCreatorUser = true;
                }

                // Save task notes and user information, returns TaskUpdateId for reference to add in user attachments.
                Int32 TaskUpdateId = SaveTaskNote(Convert.ToInt64(hdnTaskId.Value), isCreatorUser, null, string.Empty);

                // Save task related user's attachment.
                UploadUserAttachements(TaskUpdateId);

                LoadTaskData(hdnTaskId.Value, false);

                txtNote.Text = string.Empty;

                //clearAllFormData();

                // Refresh task list on top header.
                //SearchTasks(null);

                //if (controlMode.Value == "0")
                //{
                //    ScriptManager.RegisterStartupScript(this.Page, GetType(), "al", "alert('Task created successfully');", true);
                //}
                //else
                //{
                //   ScriptManager.RegisterStartupScript(this.Page, GetType(), "al", "alert('Task updated successfully');", true);
                //}

            }
        }

        private void UploadUserAttachements(int taskUpdateId)
        {
            //User has attached file than save it to database.
            if (!String.IsNullOrEmpty(hdnAttachments.Value))
            {
                TaskUser taskUserFiles = new TaskUser();

                String[] files = hdnAttachments.Value.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (String attachment in files)
                {
                    taskUserFiles.Attachment = attachment;
                    taskUserFiles.Mode = 0; // insert data.
                    taskUserFiles.TaskId = Convert.ToInt64(hdnTaskId.Value);
                    taskUserFiles.UserId = Convert.ToInt32(Session[JG_Prospect.Common.SessionKey.Key.UserId.ToString()]);
                    taskUserFiles.TaskUpdateId = taskUpdateId;
                    TaskGeneratorBLL.Instance.SaveOrDeleteTaskUserFiles(taskUserFiles);  // save task files
                }
            }
        }

        /// <summary>
        /// Save task user information.
        /// </summary>
        /// <param name="Designame"></param>
        /// <param name="ItaskId"></param>
        private Int32 SaveTaskNote(long ItaskId, Boolean? IsCreated, Int32? UserId, String UserName)
        {
            Int32 TaskUpdateId = 0;

            TaskUser taskUser = new TaskUser();

            if (UserId == null)
            {
                // Take logged in user's id for logging note in database.
                taskUser.UserId = Convert.ToInt32(Session[JG_Prospect.Common.SessionKey.Key.UserId.ToString()]);
                taskUser.UserFirstName = Session["Username"].ToString();
            }
            else
            {
                taskUser.UserId = Convert.ToInt32(UserId);
                taskUser.UserFirstName = UserName;
            }



            //taskUser.UserType = userType.Text;
            taskUser.Notes = txtNote.Text;

            // if user has just created task then send entry with iscreator= true to distinguish record from other user's log.

            if (IsCreated != null)
            {
                taskUser.IsCreatorUser = true;
            }
            else
            {
                taskUser.IsCreatorUser = false;
            }

            taskUser.TaskId = ItaskId;

            taskUser.Status = Convert.ToInt16(cmbStatus.SelectedItem.Value);

            int userAcceptance = Convert.ToInt32(ddlUserAcceptance.SelectedItem.Value);

            taskUser.UserAcceptance = Convert.ToBoolean(userAcceptance);

            TaskGeneratorBLL.Instance.SaveOrDeleteTaskNotes(ref taskUser);

            TaskUpdateId = taskUser.TaskUpdateId;

            //for (int i = 0; i < gdTaskUsers.Rows.Count; i++)
            //{

            //    TaskUser taskUser = new TaskUser();
            //    Label userID = (Label)gdTaskUsers.Rows[i].Cells[1].FindControl("lbluserId");
            //    Label userType = (Label)gdTaskUsers.Rows[i].Cells[1].FindControl("lbluserType");
            //    Label notes = (Label)gdTaskUsers.Rows[i].Cells[1].FindControl("lblNotes");
            //    taskUser.UserId = Convert.ToInt32(userID.Text);
            //    //taskUser.UserType = userType.Text;
            //    taskUser.Notes = notes.Text;
            //    taskUser.TaskId = ItaskId;

            //    taskUser.Status = Convert.ToInt16(cmbStatus.SelectedItem.Value);
            //    int userAcceptance = Convert.ToInt32(ddlUserAcceptance.SelectedItem.Value);
            //    taskUser.UserAcceptance = Convert.ToBoolean(userAcceptance);
            //    TaskGeneratorBLL.Instance.SaveOrDeleteTaskUser(ref taskUser);

            //    TaskUpdateId = taskUser.TaskUpdateId;

            //    //Inform user by email about task assgignment.
            //    //SendEmail(Designame, taskUser.UserId); // send auto email to selected users

            //}

            return TaskUpdateId;
        }

        /// <summary>
        /// send Email
        /// </summary>
        /// <param name="userID"></param>
        private void SendEmail(string Designation, int userID)
        {
            try
            {
                DataSet dsUser = TaskGeneratorBLL.Instance.GetInstallUserDetails(Convert.ToInt32(userID));

                string HTML_TAG_PATTERN = "<.*?>";
                DataSet ds = new DataSet(); //AdminBLL.Instance.GetEmailTemplate("Sales Auto Email");// AdminBLL.Instance.FetchContractTemplate(104);

                ds = AdminBLL.Instance.GetEmailTemplate(Designation);

                if (ds == null)
                {
                    if (Designation.Contains("Install"))
                    {
                        ds = AdminBLL.Instance.GetEmailTemplate("Installer - Helper");// AdminBLL.Instance.FetchContractTemplate(104);
                    }
                    else
                    {
                        ds = AdminBLL.Instance.GetEmailTemplate("SubContractor");// AdminBLL.Instance.FetchContractTemplate(104);
                    }
                }
                else if (ds.Tables[0].Rows.Count == 0)
                {
                    if (Designation.Contains("Install"))
                    {
                        ds = AdminBLL.Instance.GetEmailTemplate("Installer - Helper");// AdminBLL.Instance.FetchContractTemplate(104);
                    }
                    else
                    {
                        ds = AdminBLL.Instance.GetEmailTemplate("SubContractor");// AdminBLL.Instance.FetchContractTemplate(104);
                    }
                }

                string FName = dsUser.Tables[0].Rows[0]["FristName"].ToString();
                string emailId = dsUser.Tables[0].Rows[0]["Email"].ToString();
                string LName = dsUser.Tables[0].Rows[0]["LastName"].ToString();
                string fullname = FName + " " + LName;

                string strHeader = ds.Tables[0].Rows[0]["HTMLHeader"].ToString(); //GetEmailHeader(status);
                string strBody = ds.Tables[0].Rows[0]["HTMLBody"].ToString(); //GetEmailBody(status);
                string strFooter = ds.Tables[0].Rows[0]["HTMLFooter"].ToString(); // GetFooter(status);
                string strsubject = ds.Tables[0].Rows[0]["HTMLSubject"].ToString();

                string userName = ConfigurationManager.AppSettings["VendorCategoryUserName"].ToString();
                string password = ConfigurationManager.AppSettings["VendorCategoryPassword"].ToString();




                strBody = strBody.Replace("#Name#", FName).Replace("#name#", FName);
                /*
                 strBody = strBody.Replace("#Date#", dtInterviewDate.Text).Replace("#date#", dtInterviewDate.Text);
                 strBody = strBody.Replace("#Time#", ddlInsteviewtime.SelectedValue).Replace("#time#", ddlInsteviewtime.SelectedValue);
                  */
                strBody = strBody.Replace("#Designation#", Designation).Replace("#designation#", Designation);


                strFooter = strFooter.Replace("#Name#", FName).Replace("#name#", FName);
                /*
                strFooter = strFooter.Replace("#Date#", dtInterviewDate.Text).Replace("#date#", dtInterviewDate.Text);
                strFooter = strFooter.Replace("#Time#", ddlInsteviewtime.SelectedValue).Replace("#time#", ddlInsteviewtime.SelectedValue);
                 */
                strFooter = strFooter.Replace("#Designation#", Designation).Replace("#designation#", Designation);

                strBody = strBody.Replace("Lbl Full name", fullname);
                strBody = strBody.Replace("LBL position", Designation);
                //strBody = strBody.Replace("lbl: start date", txtHireDate.Text);
                //strBody = strBody.Replace("($ rate","$"+ txtHireDate.Text);
                /*
                strBody = strBody.Replace("Reason", Reason);
                */
                //Hi #lblFName#, <br/><br/>You are requested to appear for an interview on #lblDate# - #lblTime#.<br/><br/>Regards,<br/>
                StringBuilder Body = new StringBuilder();
                MailMessage Msg = new MailMessage();
                //Sender e-mail address.
                Msg.From = new MailAddress(userName, "JGrove Construction");
                //ds = AdminBLL.Instance.GetEmailTemplate('');
                Msg.To.Add(emailId);
                Msg.Bcc.Add(new MailAddress("shabbir.kanchwala@straitapps.com", "Shabbir Kanchwala"));
                Msg.CC.Add(new MailAddress("jgrove.georgegrove@gmail.com", "Justin Grove"));

                Msg.Subject = strsubject;// "JG Prospect Notification";
                Body.Append(strHeader);
                Body.Append(strBody);
                Body.Append(strFooter);
                /*
                if (status == "OfferMade")
                {
                    createForeMenForJobAcceptance(Convert.ToString(Body), FName, LName, Designition, emailId, HireDate, EmpType, PayRates);
                }
                if (status == "Deactive")
                {
                    CreateDeactivationAttachment(Convert.ToString(Body), FName, LName, Designition, emailId, HireDate, EmpType, PayRates);
                }
                 */
                Msg.Body = Convert.ToString(Body);
                Msg.IsBodyHtml = true;
                // your remote SMTP server IP.
                for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                {
                    string sourceDir = Server.MapPath(ds.Tables[1].Rows[i]["DocumentPath"].ToString());
                    if (File.Exists(sourceDir))
                    {
                        Attachment attachment = new Attachment(sourceDir);
                        attachment.Name = Path.GetFileName(sourceDir);
                        Msg.Attachments.Add(attachment);
                    }
                }
                SmtpClient sc = new SmtpClient(ConfigurationManager.AppSettings["smtpHost"].ToString(), Convert.ToInt32(ConfigurationManager.AppSettings["smtpPort"].ToString()));


                NetworkCredential ntw = new System.Net.NetworkCredential(userName, password);
                sc.UseDefaultCredentials = false;
                sc.Credentials = ntw;

                sc.DeliveryMethod = SmtpDeliveryMethod.Network;
                sc.EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["enableSSL"].ToString()); // runtime encrypt the SMTP communications using SSL
                try
                {
                    sc.Send(Msg);
                }
                catch (Exception ex)
                {
                }

                Msg = null;
                sc.Dispose();
                sc = null;

                //  Page.RegisterStartupScript("UserMsg", "<script>alert('An email notification has sent on " + emailId + ".');}</script>");

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        private void SendEmailToAssignedUsers(string strInstallUserIDs)
        {
            try
            {
                string strHTMLTemplateName = "Task Generator Auto Email";
                DataSet dsEmailTemplate = AdminBLL.Instance.GetEmailTemplate(strHTMLTemplateName);
                foreach (string userID in strInstallUserIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    DataSet dsUser = TaskGeneratorBLL.Instance.GetInstallUserDetails(Convert.ToInt32(userID));

                    string emailId = dsUser.Tables[0].Rows[0]["Email"].ToString();
                    string FName = dsUser.Tables[0].Rows[0]["FristName"].ToString();
                    string LName = dsUser.Tables[0].Rows[0]["LastName"].ToString();
                    string fullname = FName + " " + LName;

                    string strHeader = dsEmailTemplate.Tables[0].Rows[0]["HTMLHeader"].ToString();
                    string strBody = dsEmailTemplate.Tables[0].Rows[0]["HTMLBody"].ToString();
                    string strFooter = dsEmailTemplate.Tables[0].Rows[0]["HTMLFooter"].ToString();
                    string strsubject = dsEmailTemplate.Tables[0].Rows[0]["HTMLSubject"].ToString();

                    strBody = strBody.Replace("#Name#", fullname);
                    //strBody = string.Format(
                    //                            "Hi {0},<br/><br/>You are assigned a task.<br/><br/>Please login to view assigned tasks.<br/><br/>Thanks!",
                    //                            FName
                    //                        );
                    strBody = strHeader + strBody + strFooter;
                    //strFooter = strFooter.Replace("#Name#", FName).Replace("#name#", FName);

                    List<Attachment> lstAttachments = new List<Attachment>();
                    // your remote SMTP server IP.
                    for (int i = 0; i < dsEmailTemplate.Tables[1].Rows.Count; i++)
                    {
                        string sourceDir = Server.MapPath(dsEmailTemplate.Tables[1].Rows[i]["DocumentPath"].ToString());
                        if (File.Exists(sourceDir))
                        {
                            Attachment attachment = new Attachment(sourceDir);
                            attachment.Name = Path.GetFileName(sourceDir);
                            lstAttachments.Add(attachment);
                        }
                    }
                    CommonFunction.SendEmail(strHTMLTemplateName, emailId, strsubject, strBody, lstAttachments);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        private void LoadTaskData(string TaskId, Boolean LoadPopup)
        {
            DataSet dsTaskDetails = TaskGeneratorBLL.Instance.GetTaskDetails(Convert.ToInt32(TaskId));

            DataTable dtTaskMasterDetails = dsTaskDetails.Tables[0];

            DataTable dtTaskDesignationDetails = dsTaskDetails.Tables[1];

            DataTable dtTaskAssignedUserDetails = dsTaskDetails.Tables[2];

            DataTable dtTaskNotesDetails = dsTaskDetails.Tables[3];

            DataTable dtSubTaskDetails = dsTaskDetails.Tables[4];

            SetSubTaskSectionView(true);

            SetMasterTaskDetails(dtTaskMasterDetails);
            SetTaskDesignationDetails(dtTaskDesignationDetails);
            SetTaskAssignedUsers(dtTaskAssignedUserDetails);
            SetTaskUserNNotesDetails(dtTaskNotesDetails);
            SetSubTaskDetails(dtSubTaskDetails);

            String taskPopupTitle = GetTaskPopupTitle(TaskId, dtTaskMasterDetails);

            if (LoadPopup)
            {
                ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "open popup", "EditTask(0, \"" + taskPopupTitle + "\");", true);
            }
        }

        private void SetSubTaskSectionView(bool blnView)
        {
            trSubTaskList.Visible = blnView;
        }

        private string GetTaskPopupTitle(String TaskId, DataTable dtTaskMasterDetails)
        {

            StringBuilder taskTitle = new StringBuilder();
            taskTitle.Append("<table width='100%'>");
            taskTitle.Append("<tr>");

            // If its admin then add delete button else not delete button for normal users.
            if (this.IsAdminMode)
            {
                taskTitle.Append(String.Concat("<td width='25%' align='left'><a style='cursor: pointer;' onclick='javascript: RemoveTask(", TaskId, ");'>Delete</a>&nbsp;&nbsp;Task ID#: **ID**</td>"));
            }
            else
            {
                taskTitle.Append("<td width='25%' align='left'>Task ID#: **ID**</td>");
            }

            taskTitle.Append("<td align='center'>Date Created: **CREATED**</td>");
            taskTitle.Append("<td width='25%' align='right'>**MNGR**</td>");
            taskTitle.Append("</tr>");
            taskTitle.Append("</table>");
            taskTitle = taskTitle.Replace("**ID**", dtTaskMasterDetails.Rows[0]["InstallId"].ToString()).Replace("**CREATED**", CommonFunction.FormatDateTimeString(dtTaskMasterDetails.Rows[0]["CreatedOn"]));

            if (dtTaskMasterDetails.Rows[0]["AssigningManager"] != null && !String.IsNullOrEmpty(dtTaskMasterDetails.Rows[0]["AssigningManager"].ToString()))
            {
                taskTitle = taskTitle.Replace("**MNGR**", String.Concat("Created By: ", dtTaskMasterDetails.Rows[0]["AssigningManager"].ToString()));
            }
            else
            {
                taskTitle = taskTitle.Replace("**MNGR**", string.Empty);
            }

            return taskTitle.ToString();
        }

        private void SetTaskAssignedUsers(DataTable dtTaskAssignedUserDetails)
        {
            String firstAssignedUser = String.Empty;
            foreach (DataRow row in dtTaskAssignedUserDetails.Rows)
            {

                ListItem item = ddcbAssigned.Items.FindByValue(row["UserId"].ToString());

                if (item != null)
                {
                    item.Selected = true;

                    if (string.IsNullOrEmpty(firstAssignedUser))
                    {
                        firstAssignedUser = item.Text;
                    }
                }
            }

            if (!String.IsNullOrEmpty(firstAssignedUser))
            {
                ddcbAssigned.Texts.SelectBoxCaption = firstAssignedUser;
            }
            else
            {
                ddcbAssigned.Texts.SelectBoxCaption = "--Open--";
            }
        }

        private void SetTaskDesignationDetails(DataTable dtTaskDesignationDetails)
        {
            String firstDesignation = string.Empty;
            if (this.IsAdminMode)
            {
                foreach (DataRow row in dtTaskDesignationDetails.Rows)
                {

                    ListItem item = ddlUserDesignation.Items.FindByText(row["Designation"].ToString());

                    if (item != null)
                    {
                        item.Selected = true;

                        if (string.IsNullOrEmpty(firstDesignation))
                        {
                            firstDesignation = item.Text;
                        }
                    }
                }

                ddlUserDesignation.Texts.SelectBoxCaption = firstDesignation;

                LoadUsersByDesgination();
            }
            else
            {
                StringBuilder designations = new StringBuilder(string.Empty);

                foreach (DataRow row in dtTaskDesignationDetails.Rows)
                {
                    designations.Append(String.Concat(row["Designation"].ToString(), ","));
                }

                ltlTUDesig.Text = String.IsNullOrEmpty(designations.ToString()) == true ? string.Empty : designations.ToString().Substring(0, designations.ToString().Length - 1);
            }
        }

        private void SetTaskUserNNotesDetails(DataTable dtTaskUserDetails)
        {
            gdTaskUsers.DataSource = dtTaskUserDetails;
            gdTaskUsers.DataBind();
        }

        private void SetSubTaskDetails(DataTable dtSubTaskDetails)
        {
            gvSubTasks.DataSource = dtSubTaskDetails;
            gvSubTasks.DataBind();
            upSubTasks.Update();

            if (dtSubTaskDetails.Rows.Count > 0)
            {
                this.LastSubTaskSequence = dtSubTaskDetails.Rows[dtSubTaskDetails.Rows.Count - 1]["InstallId"].ToString();

            }



        }

        private void SetMasterTaskDetails(DataTable dtTaskMasterDetails)
        {
            if (this.IsAdminMode)
            {
                txtTaskTitle.Text = Server.HtmlDecode(dtTaskMasterDetails.Rows[0]["Title"].ToString());
                txtDescription.Text = Server.HtmlDecode(dtTaskMasterDetails.Rows[0]["Description"].ToString());

                //Get selected index of task status
                ListItem item = cmbStatus.Items.FindByValue(dtTaskMasterDetails.Rows[0]["Status"].ToString());

                if (item != null)
                {
                    cmbStatus.SelectedIndex = cmbStatus.Items.IndexOf(item);
                }

                txtDueDate.Text = CommonFunction.FormatToShortDateString(dtTaskMasterDetails.Rows[0]["DueDate"]);
                txtHours.Text = dtTaskMasterDetails.Rows[0]["Hours"].ToString();

                //hide user view table.
                tblUserTaskView.Visible = false;
            }
            else
            {
                //hide admin view table.
                tblAdminTaskView.Visible = false;
                toggleValidators(false);

                ltlTUTitle.Text = dtTaskMasterDetails.Rows[0]["Title"].ToString();
                txtTUDesc.Text = dtTaskMasterDetails.Rows[0]["Description"].ToString();

                //Get selected index of task status
                ListItem item = ddlTUStatus.Items.FindByValue(dtTaskMasterDetails.Rows[0]["Status"].ToString());

                if (item != null)
                {
                    ddlTUStatus.SelectedIndex = cmbStatus.Items.IndexOf(item);
                }

                ltlTUDueDate.Text = CommonFunction.FormatToShortDateString(dtTaskMasterDetails.Rows[0]["DueDate"]);
                ltlTUHrsTask.Text = dtTaskMasterDetails.Rows[0]["Hours"].ToString();


            }
            // ddlUserDesignation.SelectedValue = dtTaskMasterDetails.Rows[0]["Designation"].ToString();
            //LoadUsersByDesgination();
        }

        private void toggleValidators(bool flag)
        {
            rfvTaskTitle.Visible = flag;
            rfvDesc.Visible = flag;
            cvDesignations.Visible = flag;
        }

        private void DownloadUserAttachments(String CommaSeperatedFiles)
        {
            string[] files = CommaSeperatedFiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            //var archive = Server.MapPath("~/TaskAttachments/archive.zip");
            //var temp = Server.MapPath("~/TaskAttachments/temp");

            //// clear any existing archive
            //if (System.IO.File.Exists(archive))
            //{
            //    System.IO.File.Delete(archive);
            //}

            //// empty the temp folder
            //Directory.EnumerateFiles(temp).ToList().ForEach(f => System.IO.File.Delete(f));

            //// copy the selected files to the temp folder
            //foreach (var file in files)
            //{
            //    System.IO.File.Copy(file, Path.Combine(temp, Path.GetFileName(file)));
            //}

            //// create a new archive
            //ZipFile.CreateFromDirectory(temp, archive);

            using (ZipFile zip = new ZipFile())
            {
                foreach (var file in files)
                {
                    string filePath = Server.MapPath("~/TaskAttachments/" + file);
                    zip.AddFile(filePath, "files");
                }

                Response.Clear();
                Response.AddHeader("Content-Disposition", "attachment; filename=DownloadedFile.zip");
                Response.ContentType = "application/zip";
                zip.Save(Response.OutputStream);

                Response.End();


            }
        }

        private void SetTaskView()
        {
            if (this.IsAdminMode)
            {
                tblAdminTaskView.Visible = true;
                tblUserTaskView.Visible = false;
            }
            else
            {
                tblAdminTaskView.Visible = false;
                tblUserTaskView.Visible = true;
            }
        }

        #endregion
    }
}