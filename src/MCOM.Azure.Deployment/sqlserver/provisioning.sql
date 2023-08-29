/*Assign permissions to logic app managed identity*/
CREATE USER [logig-mcom-provisioning-inttest] FROM  EXTERNAL PROVIDER  WITH DEFAULT_SCHEMA=[dbo]
GO

ALTER ROLE [db_datawriter] ADD MEMBER [logig-mcom-provisioning-inttest]
GO

/****** Object:  Table [dbo].[ProvisioningRequests]    Script Date: 28.08.2023 13:16:06 ******/
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