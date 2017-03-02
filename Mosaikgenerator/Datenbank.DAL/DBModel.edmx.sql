
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 03/02/2017 17:59:46
-- Generated from EDMX file: D:\GitProjects\VS2016\Mosaikgenerator\Datenbank.DAL\DBModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [VWW1];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_PoolsImages]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ImagesSet] DROP CONSTRAINT [FK_PoolsImages];
GO
IF OBJECT_ID(N'[dbo].[FK_Kacheln_inherits_Images]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ImagesSet_Kacheln] DROP CONSTRAINT [FK_Kacheln_inherits_Images];
GO
IF OBJECT_ID(N'[dbo].[FK_Motive_inherits_Images]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ImagesSet_Motive] DROP CONSTRAINT [FK_Motive_inherits_Images];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[PoolsSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PoolsSet];
GO
IF OBJECT_ID(N'[dbo].[ImagesSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ImagesSet];
GO
IF OBJECT_ID(N'[dbo].[ImagesSet_Kacheln]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ImagesSet_Kacheln];
GO
IF OBJECT_ID(N'[dbo].[ImagesSet_Motive]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ImagesSet_Motive];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'PoolsSet'
CREATE TABLE [dbo].[PoolsSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [name] nvarchar(max)  NOT NULL,
    [owner] nvarchar(max)  NOT NULL,
    [size] int  NOT NULL,
    [writelock] bit  NOT NULL
);
GO

-- Creating table 'ImagesSet'
CREATE TABLE [dbo].[ImagesSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [PoolsId] int  NOT NULL,
    [path] nvarchar(max)  NOT NULL,
    [filename] nvarchar(max)  NOT NULL,
    [displayname] nvarchar(max)  NOT NULL,
    [width] int  NOT NULL,
    [heigth] int  NOT NULL,
    [hsv] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'ImagesSet_Kacheln'
CREATE TABLE [dbo].[ImagesSet_Kacheln] (
    [avgR] int  NOT NULL,
    [avgG] int  NOT NULL,
    [avgB] int  NOT NULL,
    [Id] int  NOT NULL
);
GO

-- Creating table 'ImagesSet_Motive'
CREATE TABLE [dbo].[ImagesSet_Motive] (
    [readlock] bit  NOT NULL,
    [writelock] bit  NOT NULL,
    [Id] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'PoolsSet'
ALTER TABLE [dbo].[PoolsSet]
ADD CONSTRAINT [PK_PoolsSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ImagesSet'
ALTER TABLE [dbo].[ImagesSet]
ADD CONSTRAINT [PK_ImagesSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ImagesSet_Kacheln'
ALTER TABLE [dbo].[ImagesSet_Kacheln]
ADD CONSTRAINT [PK_ImagesSet_Kacheln]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ImagesSet_Motive'
ALTER TABLE [dbo].[ImagesSet_Motive]
ADD CONSTRAINT [PK_ImagesSet_Motive]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [PoolsId] in table 'ImagesSet'
ALTER TABLE [dbo].[ImagesSet]
ADD CONSTRAINT [FK_PoolsImages]
    FOREIGN KEY ([PoolsId])
    REFERENCES [dbo].[PoolsSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_PoolsImages'
CREATE INDEX [IX_FK_PoolsImages]
ON [dbo].[ImagesSet]
    ([PoolsId]);
GO

-- Creating foreign key on [Id] in table 'ImagesSet_Kacheln'
ALTER TABLE [dbo].[ImagesSet_Kacheln]
ADD CONSTRAINT [FK_Kacheln_inherits_Images]
    FOREIGN KEY ([Id])
    REFERENCES [dbo].[ImagesSet]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Id] in table 'ImagesSet_Motive'
ALTER TABLE [dbo].[ImagesSet_Motive]
ADD CONSTRAINT [FK_Motive_inherits_Images]
    FOREIGN KEY ([Id])
    REFERENCES [dbo].[ImagesSet]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------