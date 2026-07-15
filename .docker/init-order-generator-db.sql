-- Create database if not exists
CREATE DATABASE order_generator;

-- Connect to the new database
\c order_generator;

-- Create Orders table (EF Core expects uppercase)
CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" UUID PRIMARY KEY,
    "Symbol" VARCHAR(10) NOT NULL,
    "Side" VARCHAR(10) NOT NULL,
    "Quantity" BIGINT NOT NULL,
    "Price" NUMERIC(18,2) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NOT NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_Orders_CreatedAt" ON "Orders"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Orders_Symbol" ON "Orders"("Symbol");
