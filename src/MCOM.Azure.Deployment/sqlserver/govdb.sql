/****** Object:  Table [dbo].[MCOMScanRequestMessage]    Script Date: 27.04.2022 11:14:17 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MCOMScanRequest](
	[id] [uniqueidentifier] NOT NULL,
	[requester] [nvarchar](150) NULL,
	[webid] [nvarchar](255) NULL,
	[listid] [nvarchar](255) NULL,
	[siteid] [nvarchar](255) NULL,
	[requestdate] [datetime] NULL,
	[wbs] [nvarchar](100) NULL,
	[businessunit] [nvarchar](255) NULL,
	[itemid] [int] NULL,
	[documentname] [nvarchar](max) NULL,
	[vendor] [nvarchar](255) NULL,
	[comments] [nvarchar](max) NULL,
	[ordernumber] [nvarchar](50) NULL,
	[status] [nvarchar](50) NULL,
	[isphysical] [bit] NULL,
 CONSTRAINT [PK_MCOMScanRequest] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MCOMScanExecution](
	[filename] [nvarchar](255) NULL,
	[datescanned] [datetime] NULL,
	[requestId] [uniqueidentifier] NULL,
	[size] [int] NULL
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[MCOMScanRequestMessage](
	[id] [uniqueidentifier] NOT NULL,
	[creator] [nvarchar](255) NULL,
	[createddate] [datetime] NULL,
	[description] [nvarchar](max) NULL,
	[requestid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_MCOMScanRequestMessage] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[MCOMScanRequestMessage]  WITH CHECK ADD  CONSTRAINT [FK_RequestMessage_Request] FOREIGN KEY([requestid])
REFERENCES [dbo].[MCOMScanRequest] ([id])
GO

ALTER TABLE [dbo].[MCOMScanRequestMessage] CHECK CONSTRAINT [FK_RequestMessage_Request]
GO

ALTER TABLE [dbo].[MCOMScanExecution]  WITH CHECK ADD  CONSTRAINT [FK_MCOMScanExecution_MCOMScanRequest] FOREIGN KEY([requestId])
REFERENCES [dbo].[MCOMScanRequest] ([id])
GO

ALTER TABLE [dbo].[MCOMScanExecution] CHECK CONSTRAINT [FK_MCOMScanExecution_MCOMScanRequest]
GO


/****** Stored Procedures ******/

-- =============================================
-- Author:      Carlos Briceno
-- Create Date: 27.04.2022
-- Description: Insert or update a scan execution record
-- =============================================
CREATE PROCEDURE [dbo].[upsert_scanexecution]
(
    -- Add the parameters for the stored procedure here
    @pRequestId varchar(50) = null,
    @pScannedDate datetime = null,
    @pSize int = null,
	@pFileName varchar(255) = null
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON;

    BEGIN TRANSACTION

        UPDATE dbo.MCOMScanExecution WITH (SERIALIZABLE)
        SET datescanned = @pScannedDate,
            size = @pSize,
			filename =	@pFileName
        WHERE @pRequestId = @pRequestId

        IF @@rowcount = 0
            INSERT INTO dbo.MCOMScanExecution (RequestId, datescanned, size, filename)
            VALUES (@pRequestId, @pScannedDate, @pSize, @pFileName)

    COMMIT TRANSACTION
END
GO


/****** Users and roles ******/
CREATE USER [function-mcom-scanondemand-inttest] FROM  EXTERNAL PROVIDER  WITH DEFAULT_SCHEMA=[dbo]
GO

ALTER ROLE [db_datareader] ADD MEMBER [function-mcom-scanondemand-inttest]
GO

CREATE USER [adf-mcom-inttest] FROM  EXTERNAL PROVIDER  WITH DEFAULT_SCHEMA=[dbo]
GO

ALTER ROLE [db_owner] ADD MEMBER [adf-mcom-inttest]
GO


