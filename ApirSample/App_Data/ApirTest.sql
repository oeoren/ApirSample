IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products
GO
IF OBJECT_ID('dbo.API_Products_Put', 'P') IS NOT NULL DROP PROCEDURE dbo.API_Products_Put
GO
IF OBJECT_ID('dbo.API_Products_Get', 'P') IS NOT NULL DROP PROCEDURE dbo.API_Products_Get
GO
IF OBJECT_ID('dbo.API_Products_Post', 'P') IS NOT NULL DROP PROCEDURE dbo.API_Products_Post
GO
IF OBJECT_ID('dbo.API_Products_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.API_Products_Delete
GO

CREATE TABLE Products
(
	ProductID int IDENTITY(1,1) PRIMARY KEY,
	ProductName varchar(100) NOT NULL
)
GO

INSERT INTO Products(ProductName) VALUES ('Widget 1') , ('Widget 2'), ('Widget 3')
GO

CREATE PROCEDURE API_Products_Get (@ID int = NULL) 
AS
    SELECT ProductId, ProductName 
        FROM Products 
        WHERE ProductID = @ID OR  @ID IS NULL
GO

CREATE PROCEDURE API_Products_Put(@ID int, @ProductName VARCHAR(100))
AS
    IF NOT EXISTS(SELECT ProductID FROM Products 
    WHERE ProductID = @ID) 
    BEGIN
            RAISERROR('Unknown Product',1,1)
            RETURN 400
    END
    UPDATE Products SET ProductName = @ProductName 
    WHERE ProductID = @ID
    RETURN 200 
GO

--- <summary> 
--- Add a new product to the database
--- </summary>  
--- <remarks> A link to the new product is added  </remarks> 
--- <response code="201">OK</response>
--- <response code="521">Bad productname</response>
--- <response code="522">Product with name exists</response>
CREATE PROCEDURE API_Products_Post(
@ProductName VARCHAR(100), @NewId int OUTPUT)
AS
	IF (@ProductName IS NULL OR LEN(@ProductName) < 2) 
    BEGIN
            RAISERROR('Bad product name',1,1)
            RETURN 521
    END
	IF EXISTS (SELECT ProductId FROM Products WHERE ProductName = @ProductName)
    BEGIN
            RAISERROR('Product with that name already exists',1,1)
            RETURN 522
    END

	INSERT INTO Products(ProductName) VALUES(@ProductName)
	SET @NewId  = @@IDENTITY
	RETURN 201
GO

CREATE PROCEDURE API_Products_Delete(@ID int)
AS
	DELETE FROM Products WHERE ProductID = @ID
GO
