/* To execute this script, use the following example command:
sqlcmd -S server\instance -E -v logicappmanagedidentity="logig-mcom-provisioning-inttest" -i provisioning.sql
*/

/*Assign permissions to logic app managed identity*/
CREATE USER $(logicappmanagedidentity) FROM  EXTERNAL PROVIDER  WITH DEFAULT_SCHEMA=[dbo]
GO

ALTER ROLE [db_datawriter] ADD MEMBER $(logicappmanagedidentity)
GO

GRANT Execute TO $(logicappmanagedidentity)
GO

/****** Object:  Table [dbo].[BusinessAreas]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BusinessAreas](
	[BA_id] [int] IDENTITY(1,1) NOT NULL,
	[BA_short_name] [varchar](255) NULL,
	[BA_full_name] [varchar](255) NULL,
	[BA_active] [bit] NULL,
 CONSTRAINT [PK_BusinessAreas] PRIMARY KEY CLUSTERED 
(
	[BA_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningAccessLevels]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningAccessLevels](
	[access_level_id] [int] IDENTITY(1,1) NOT NULL,
	[access_level_name] [nvarchar](255) NOT NULL,
	[access_level_definition] [nvarchar](500) NULL,
 CONSTRAINT [PK_ProvisioningAccessLevels] PRIMARY KEY CLUSTERED 
(
	[access_level_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningAppContent]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningAppContent](
	[screen_id] [int] IDENTITY(1,1) NOT NULL,
	[screen_name] [varchar](255) NULL,
	[screen_description] [nvarchar](max) NULL,
 CONSTRAINT [PK_ProvisioningAppContent] PRIMARY KEY CLUSTERED 
(
	[screen_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningLogErrors]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningLogErrors](
	[logerror_id] [int] IDENTITY(1,1) NOT NULL,
	[logerror_name] [varchar](500) NULL,
	[logerror_description] [nvarchar](max) NULL,
	[logerror_timestamp] [datetime2](7) NULL,
	[request_id] [int] NULL,
 CONSTRAINT [PK_ProvisioningLogErrors] PRIMARY KEY CLUSTERED 
(
	[logerror_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningMessageTeamplates]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningMessageTeamplates](
	[msg_id] [int] IDENTITY(1,1) NOT NULL,
	[msg_name] [varchar](255) NULL,
	[msg_subject] [nvarchar](max) NULL,
	[msg_body] [nvarchar](max) NULL,
	[msg_importance] [bit] NULL,
 CONSTRAINT [PK_ProvisioningMessageTeamplates] PRIMARY KEY CLUSTERED 
(
	[msg_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningPurposes]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningPurposes](
	[purpose_id] [int] IDENTITY(1,1) NOT NULL,
	[purpose_name] [varchar](255) NOT NULL,
	[purpose_description] [nvarchar](max) NULL,
	[purpose_active] [bit] NULL,
 CONSTRAINT [PK_ProvisioningPurposes] PRIMARY KEY CLUSTERED 
(
	[purpose_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningRecommendationScore]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningRecommendationScore](
	[score_id] [int] IDENTITY(1,1) NOT NULL,
	[score] [int] NOT NULL,
	[workload_id] [int] NULL,
	[purpose_id] [int] NULL,
 CONSTRAINT [PK_ProvisioningRecommendationScore] PRIMARY KEY CLUSTERED 
(
	[score_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningRequests]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningRequests](
	[request_id] [int] IDENTITY(1,1) NOT NULL,
	[requestor_email] [nvarchar](255) NOT NULL,
	[requestor_ba_id] [int] NULL,
	[requestor_role_id] [int] NULL,
	[request_status] [varchar](255) NOT NULL,
	[request_date] [datetime2](7) NOT NULL,
	[request_ordered_thru] [varchar](255) NULL,
	[message_sent] [bit] NULL,
	[request_purposes] [nvarchar](max) NULL,
	[approver_email] [nvarchar](255) NULL,
	[approver_comments] [nvarchar](500) NULL,
	[request_is_bulk] [bit] NOT NULL,
 CONSTRAINT [PK_ProvisioningRequests] PRIMARY KEY CLUSTERED 
(
	[request_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningReservedNames]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningReservedNames](
	[reserved_name_id] [int] IDENTITY(1,1) NOT NULL,
	[reserved_name_desc] [varchar](55) NULL,
	[reserved_names] [nvarchar](max) NULL,
	[role_id] [int] NULL,
 CONSTRAINT [PK_ProvisioningReservedNames] PRIMARY KEY CLUSTERED 
(
	[reserved_name_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningRoles]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningRoles](
	[role_id] [int] IDENTITY(1,1) NOT NULL,
	[role_name] [nvarchar](255) NULL,
	[role_group_id] [char](36) NULL,
	[role_group_name] [nvarchar](255) NULL,
	[role_active] [bit] NULL,
	[access_level_id] [int] NULL,
 CONSTRAINT [PK_ProvisioningRoles] PRIMARY KEY CLUSTERED 
(
	[role_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningSiteOwnership]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningSiteOwnership](
	[siteownership_id] [int] IDENTITY(1,1) NOT NULL,
	[site_id] [int] NULL,
	[group_owners] [varchar](500) NULL,
	[group_owners_email] [varchar](500) NULL,
	[group_members_count] [int] NULL,
	[site_owners] [varchar](500) NULL,
	[site_owners_email] [varchar](500) NULL,
	[site_members_count] [int] NULL,
	[everyone_has_access] [bit] NULL,
 CONSTRAINT [PK_ProvisioningSiteOwnership] PRIMARY KEY CLUSTERED 
(
	[siteownership_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningSitesInformation]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningSitesInformation](
	[site_id] [int] IDENTITY(1,1) NOT NULL,
	[site_guid] [char](36) NOT NULL,
	[request_id] [int] NULL,
	[workload_id] [int] NULL,
	[template_id] [int] NULL,
	[site_status] [varchar](255) NULL,
	[site_title] [nvarchar](255) NOT NULL,
	[site_address] [varchar](500) NULL,
	[site_template] [varchar](255) NULL,
	[site_description] [nvarchar](255) NULL,
	[site_lockstate] [varchar](255) NULL,
	[site_readonly] [bit] NULL,
	[site_sensitivity] [varchar](255) NULL,
	[site_vissibility] [varchar](255) NULL,
	[site_external_sharing] [varchar](255) NULL,
	[site_lifespan] [varchar](255) NULL,
	[site_locale] [varchar](255) NULL,
	[site_timezone] [varchar](255) NULL,
	[site_last_activity] [datetime2](7) NULL,
	[site_created_date] [datetime2](7) NULL,
	[site_created_by] [nvarchar](255) NULL,
	[site_provisioned_date] [datetime2](7) NULL,
	[site_provisioned_by] [nvarchar](255) NULL,
	[site_deleted_date] [datetime2](7) NULL,
	[site_deleted_by] [nvarchar](255) NULL,
	[hub_id] [char](36) NULL,
	[hub_connected] [bit] NULL,
	[group_id] [char](36) NULL,
	[group_name] [nvarchar](255) NULL,
	[group_connected] [bit] NULL,
	[group_email] [nvarchar](255) NULL,
	[group_created_date] [datetime2](7) NULL,
	[team_id] [char](36) NULL,
	[team_connected] [bit] NULL,
	[standard_channel_count] [int] NULL,
	[private_channel_count] [int] NULL,
	[shared_channel_count] [int] NULL,
	[channel_id] [char](36) NULL,
	[channel_connected] [bit] NULL,
	[channel_type] [varchar](255) NULL,
	[yammer_connected] [bit] NULL,
 CONSTRAINT [PK_ProvisioningSitesInformation] PRIMARY KEY CLUSTERED 
(
	[site_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningSitesMetadata]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningSitesMetadata](
	[site_metadata_id] [int] IDENTITY(1,1) NOT NULL,
	[site_id] [int] NULL,
	[business_area] [varchar](255) NULL,
	[country] [varchar](255) NULL,
	[legal_entity] [varchar](255) NULL,
	[business_capability_level_1] [varchar](255) NULL,
	[business_capability_level_2] [varchar](255) NULL,
	[information_type] [varchar](255) NULL,
	[security_classification] [varchar](255) NULL,
	[status] [varchar](255) NULL,
	[source] [varchar](255) NULL,
	[site_origin_source] [varchar](255) NULL,
	[organisation_unit] [varchar](255) NULL,
	[basin] [varchar](255) NULL,
	[block] [varchar](255) NULL,
	[business_arrangement_area] [varchar](255) NULL,
	[continent] [varchar](255) NULL,
	[counterparty] [varchar](255) NULL,
	[decision_gate] [varchar](255) NULL,
	[discipline] [varchar](255) NULL,
	[employee_number] [varchar](255) NULL,
	[field] [varchar](255) NULL,
	[license] [varchar](255) NULL,
	[marketing_product] [varchar](255) NULL,
	[plant] [varchar](255) NULL,
	[plant_type] [varchar](255) NULL,
	[seismic_survey] [varchar](255) NULL,
	[well] [varchar](255) NULL,
	[wellbore] [varchar](255) NULL,
	[well_intervention] [varchar](255) NULL,
	[latitude] [varchar](255) NULL,
	[longitude] [varchar](255) NULL,
	[project_pm_number] [varchar](255) NULL,
	[storage_usage] [varchar](255) NULL,
	[number_of_subsites] [int] NULL,
	[number_of_files] [int] NULL,
	[files_viewed_edited] [int] NULL,
	[page_views] [int] NULL,
	[page_visits] [int] NULL,
 CONSTRAINT [PK_ProvisioningSitesMetadata] PRIMARY KEY CLUSTERED 
(
	[site_metadata_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningSupport]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningSupport](
	[support_id] [int] IDENTITY(1,1) NOT NULL,
	[support_name] [varchar](255) NULL,
	[support_description] [nvarchar](max) NULL,
	[support_link] [nvarchar](255) NULL,
 CONSTRAINT [PK_ProvisioningSupport] PRIMARY KEY CLUSTERED 
(
	[support_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningTemplates]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningTemplates](
	[template_id] [int] IDENTITY(1,1) NOT NULL,
	[template_name] [varchar](255) NOT NULL,
	[template_active] [bit] NULL,
	[template_location] [nvarchar](255) NULL,
 CONSTRAINT [PK_ProvisionintTemplates] PRIMARY KEY CLUSTERED 
(
	[template_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProvisioningWorkloads]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProvisioningWorkloads](
	[workload_id] [int] IDENTITY(1,1) NOT NULL,
	[workload_name] [varchar](255) NOT NULL,
	[workload_description] [nvarchar](500) NULL,
	[workload_active] [bit] NULL,
 CONSTRAINT [PK_Provisioning_workloads] PRIMARY KEY CLUSTERED 
(
	[workload_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SiteDirectory]    Script Date: 05.09.2023 10:23:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE EXTERNAL TABLE [dbo].[SiteDirectory]
(
	[ID] [nvarchar](50) NOT NULL,
	[sitedata] [nvarchar](max) NULL
)
WITH (DATA_SOURCE = [mydatasource1],SCHEMA_NAME = N'dbo',OBJECT_NAME = N'SiteDirectory')
GO
ALTER TABLE [dbo].[ProvisioningLogErrors]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningLogErrors_ProvisioningRequests] FOREIGN KEY([request_id])
REFERENCES [dbo].[ProvisioningRequests] ([request_id])
GO
ALTER TABLE [dbo].[ProvisioningLogErrors] CHECK CONSTRAINT [FK_ProvisioningLogErrors_ProvisioningRequests]
GO
ALTER TABLE [dbo].[ProvisioningRecommendationScore]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningRecommendationScore_ProvisioningPurposes] FOREIGN KEY([purpose_id])
REFERENCES [dbo].[ProvisioningPurposes] ([purpose_id])
GO
ALTER TABLE [dbo].[ProvisioningRecommendationScore] CHECK CONSTRAINT [FK_ProvisioningRecommendationScore_ProvisioningPurposes]
GO
ALTER TABLE [dbo].[ProvisioningRecommendationScore]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningRecommendationScore_ProvisioningWorkloads] FOREIGN KEY([workload_id])
REFERENCES [dbo].[ProvisioningWorkloads] ([workload_id])
GO
ALTER TABLE [dbo].[ProvisioningRecommendationScore] CHECK CONSTRAINT [FK_ProvisioningRecommendationScore_ProvisioningWorkloads]
GO
ALTER TABLE [dbo].[ProvisioningRequests]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningRequests_BusinessAreas] FOREIGN KEY([requestor_ba_id])
REFERENCES [dbo].[BusinessAreas] ([BA_id])
GO
ALTER TABLE [dbo].[ProvisioningRequests] CHECK CONSTRAINT [FK_ProvisioningRequests_BusinessAreas]
GO
ALTER TABLE [dbo].[ProvisioningRequests]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningRequests_ProvisioningRoles] FOREIGN KEY([requestor_role_id])
REFERENCES [dbo].[ProvisioningRoles] ([role_id])
GO
ALTER TABLE [dbo].[ProvisioningRequests] CHECK CONSTRAINT [FK_ProvisioningRequests_ProvisioningRoles]
GO
ALTER TABLE [dbo].[ProvisioningReservedNames]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningReservedNames_ProvisioningRoles] FOREIGN KEY([role_id])
REFERENCES [dbo].[ProvisioningRoles] ([role_id])
GO
ALTER TABLE [dbo].[ProvisioningReservedNames] CHECK CONSTRAINT [FK_ProvisioningReservedNames_ProvisioningRoles]
GO
ALTER TABLE [dbo].[ProvisioningRoles]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningRoles_ProvisioningAccessLevels] FOREIGN KEY([access_level_id])
REFERENCES [dbo].[ProvisioningAccessLevels] ([access_level_id])
GO
ALTER TABLE [dbo].[ProvisioningRoles] CHECK CONSTRAINT [FK_ProvisioningRoles_ProvisioningAccessLevels]
GO
ALTER TABLE [dbo].[ProvisioningSiteOwnership]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningSiteOwnership_ProvisioningSitesInformation] FOREIGN KEY([site_id])
REFERENCES [dbo].[ProvisioningSitesInformation] ([site_id])
GO
ALTER TABLE [dbo].[ProvisioningSiteOwnership] CHECK CONSTRAINT [FK_ProvisioningSiteOwnership_ProvisioningSitesInformation]
GO
ALTER TABLE [dbo].[ProvisioningSitesInformation]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningSitesInformation_ProvisioningRequests] FOREIGN KEY([request_id])
REFERENCES [dbo].[ProvisioningRequests] ([request_id])
GO
ALTER TABLE [dbo].[ProvisioningSitesInformation] CHECK CONSTRAINT [FK_ProvisioningSitesInformation_ProvisioningRequests]
GO
ALTER TABLE [dbo].[ProvisioningSitesInformation]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningSitesInformation_ProvisioningTemplates] FOREIGN KEY([template_id])
REFERENCES [dbo].[ProvisioningTemplates] ([template_id])
GO
ALTER TABLE [dbo].[ProvisioningSitesInformation] CHECK CONSTRAINT [FK_ProvisioningSitesInformation_ProvisioningTemplates]
GO
ALTER TABLE [dbo].[ProvisioningSitesInformation]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningSitesInformation_ProvisioningWorkloads] FOREIGN KEY([workload_id])
REFERENCES [dbo].[ProvisioningWorkloads] ([workload_id])
GO
ALTER TABLE [dbo].[ProvisioningSitesInformation] CHECK CONSTRAINT [FK_ProvisioningSitesInformation_ProvisioningWorkloads]
GO
ALTER TABLE [dbo].[ProvisioningSitesMetadata]  WITH CHECK ADD  CONSTRAINT [FK_ProvisioningSitesMetadata_ProvisioningSitesInformation] FOREIGN KEY([site_id])
REFERENCES [dbo].[ProvisioningSitesInformation] ([site_id])
GO
ALTER TABLE [dbo].[ProvisioningSitesMetadata] CHECK CONSTRAINT [FK_ProvisioningSitesMetadata_ProvisioningSitesInformation]
GO



