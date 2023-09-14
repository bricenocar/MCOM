-- =======================================================
-- Create Stored Procedure Template for Azure SQL Database
-- =======================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:      Carlos Briceno, Tejas Patil
-- Create Date: 12.09.2023
-- Description: Based on purpose value we calculate the recoomendation score for site template
-- =============================================
CREATE PROCEDURE [dbo].[sp-GetRecommendationScore]
(
    -- Add the parameters for the stored procedure here
	@pPurpose int = 0
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    -- Insert statements for procedure here
    BEGIN TRY
		BEGIN TRANSACTION
			-- Write logic to get recommendation score
			


		COMMIT TRANSACTION
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
	END CATCH
END
GO
