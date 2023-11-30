# ChineseCharactersApp - Showcase repo

Link to live website: https://practice-chinese.azurewebsites.net
> MVC app for practicing recognition of chinese characters, for language learners of Chinese

> The first app I built that has features that go beyond CRUD with parameters.

- C#, SQL
- .NET 7, SQL Server
- Microsoft Azure SQL Server / SQL Database / App Services (Web App)
- ClosedXML, Microsoft.Data.SqlClient

## Main Features to implement for first version public release
> specification

- must have table for the vocabulary - the characters, their pinyin and their translation minimum
- must have a users table so a vocabulary list can be associated with a specific user
- must have a table that will enable word lists to be organized into named sets
- basic features
  - user sign up, sign in, sign out
  - CRUD for vocabulary with search filter, sorting and paging parameter input
- allow users to upload their own word lists (Excel files)
- allow users to add preset word lists from a public pool
- a practice feature
  - display certain chinese characters one by one to test user's memory
  - a "spaced repetition" logic for weighted random selection of which characters to display
  - buttons for users to give input on the ease/difficulty of recognizing each displayed character
  - allow editing displayed characters "on the fly"
  - allow users to freely change their username and password
  - track and display the easiest and hardest charcters to recognize for that user
  - overview of the user's saved vocabulary

# Challenges & Learning points

## Minor points
> finding a solution how to achieve upload excel file functionality --> ClosedXML

> learning how to use Microsoft Azure to create and manage SQL Server database and Web App

> learning how to use GitHub Actions to deploy and update the app hosted on Azure directly from GH repo

> using BackgroundWorker to periodically wake up the free tier database on Azure, because the free tier automatically pauses it every hour

## Database planning
> How do I set up my database tables to respect the requirements outlined in the specification?

Following common naming convention for vocabulary recognition games, I will have users as USERS, entries in the vocabulary lists as CARDS, and collections of vocabulary lists as DECKS.

> \* however I made the vocabulary list table first and called it MainTable and will keep it as such for the time being.

```SQL
CREATE TABLE MainTable (
RowId uniqueidentifier not null PRIMARY KEY,
Simplified nvarchar(255) null,
Traditional nvarchar(255) null,
Pinyin nvarchar(255) null,
Translation nvarchar(255) null,
Note nvarchar(max) null,
TimeAdded datetime not null,
TimeUpdated datetime null,
AuthorId uniqueidentifier null, -- the user
DifficultyRating int null,
RatingMultiplier int null,
PracticeCount int null);

CREATE TABLE UserTable (
UserId uniqueidentifier not null PRIMARY KEY,
Username varchar(25) not null,
Password varchar(25) not null,
RegisteredOn datetime not null,
LastUpdated datetime null);

CREATE TABLE DeckTable (
DeckId uniqueidentifier not null,
CardId uniqueidentifier not null, -- the card in the deck
UserId uniqueidentifier not null, -- the user
DeckName nvarchar(255) null,
TextbookName nvarchar (255) null,
LessonUnit nvarchar(255) null,
TimeAdded datetime null,
TimeUpdated datetime null,
PRIMARY KEY (DeckId, CardId)); -- because DeckId will repeat for each deck and cannot be PK on its own
```
There are no unique cards or decks, each user get copies with new IDs to allow for editing and personalization.

Each user may have more than one owned deck, indicating a 1-m relationship. Each user also will have a multitude of owned cards in the MainTable, also 1-m. And finally, each deck may hold a multitude of cards AND each card may be found in multiple decks, meaning a many-to-many relationship. 

This way, there is only one instance of a user owned character and card used for scoring purposes, while multiple instances of that CardId may appear in the DeckTable, each tied to a separate specific DeckId, making a kind of ternary relationship in the DeckTable.

[image of the dekctable](link)

## Saving lists to database
> via uploading Excel files using ClosedXML
```C#
UploadExcelFile( excel object )
{
	... // maps the file to object and passes it
}
SaveToDatabase( excel object )
{
	... // receives the object, adds relevant params and sends it to the database layer
}
```
> via adding from existing lists, a public pool
```C#
ShowPresetDeckPage( params )
{
	...
	GetAllCards_MyCardsOnly(id)
	GetAllDecks_MyDecksOnly(id)
  ...
	// a DECKMASTER user is set to serve as a public pool repository of word lists
	// filters the list based on user input and passes it
}
AddPresetDeck( params )
{
	... // receives the list, maps it to necessary object and sends to database layer
}
```
> Database layer receives object and params from either controller source:
```C#
SaveToDatabase ( uploaded object with params )
{
	...
	GetAllCards_MyCardsOnly(id) //fetches all cards of the user
	FindDuplicates(all cards, uploaded object) //looks for and returns duplicates
	SaveToMainTable( various ) //saves added cards to MainTable
	SaveToDeckTable( various ) //saves added cards to DeckTable
  ...
}
```
Currently, difficulty rating is done on a single card (unique RowId) but the user sees this as a character (Simplified or Traditional), which means checks for duplicates is done on Simpified and Traditional columns and not RowId.
```C#
IEnumerable<CardDTO> duplicates = allCards.Where(x =>
    newCards.Any(y =>
        !string.IsNullOrEmpty(y.Simplified) && x.Simplified == y.Simplified
        ||
        !string.IsNullOrEmpty(y.Traditional) && x.Traditional == y.Traditional
    ) // allows for either Simplified or Traditional column to be empty/null and not count empty/null as duplicates
);
return duplicates.ToList();
```
> SaveToMainTable() will skip adding new cards (for that row) from new list if a match is found in the Simplified or Traditional columns

> SaveToDeckTable() must add all cards so that the entirety of the deck is added, but instead of generating a new CardId for a duplicate card (based on matches on Simplified/Traditional) it will use the existing RowId for the CardId - connecting an existing card/character to a new deck

> because DECKMASTER is a public pool, a check is implemented to not skip duplicates if DECKMASTER is the uploader, to make sure list-specific pinyin and translations are retained for that character

> \* one planned feature is to remove skipping duplicates, but allow users to select which field content to accept if a new deck happens to have an already added character but with a different pinyin or translation

## Practice & Scoring


