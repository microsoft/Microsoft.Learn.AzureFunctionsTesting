create table [dbo].[TestTable](
	[Id] uniqueidentifier not null
)
go

insert into TestTable (Id) values (NEWID())
go
