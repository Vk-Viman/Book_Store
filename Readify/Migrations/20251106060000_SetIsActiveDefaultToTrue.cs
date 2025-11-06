using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class SetIsActiveDefaultToTrue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
BEGIN
    DECLARE @dfname sysname;
    SELECT @dfname = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID(N'dbo.Users') AND c.name = N'IsActive';

    IF @dfname IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [dbo].[Users] DROP CONSTRAINT [' + @dfname + ']');
    END

    IF NOT EXISTS(SELECT 1 FROM sys.default_constraints d JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id WHERE d.parent_object_id = OBJECT_ID(N'dbo.Users') AND c.name = N'IsActive')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD CONSTRAINT DF_Users_IsActive DEFAULT (1) FOR IsActive;
    END
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
BEGIN
    DECLARE @dfname sysname;
    SELECT @dfname = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID(N'dbo.Users') AND c.name = N'IsActive';

    IF @dfname IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [dbo].[Users] DROP CONSTRAINT [' + @dfname + ']');
    END

    -- restore default to 0
    IF NOT EXISTS(SELECT 1 FROM sys.default_constraints d JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id WHERE d.parent_object_id = OBJECT_ID(N'dbo.Users') AND c.name = N'IsActive')
    BEGIN
        ALTER TABLE [dbo].[Users] ADD CONSTRAINT DF_Users_IsActive DEFAULT (0) FOR IsActive;
    END
END");
        }
    }
}
