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

> using BackgroundWorker to periodically wake up the database on Azure, because the free tier automatically pauses it every hour

## Database planning
> How do I set up my database tables to respect the requirements outlined in the specification?

Following common naming convention for vocabulary recognition games, I will have users as USERS, entries in the vocabulary lists as CARDS, and collections of vocabulary lists as DECKS.

> \* however I made the vocabulary list table first and called it MainTable and will keep it as such for the time being.

### Issue #1 
- the user will score how easy a character is to remember -BUT- multiple decks may contain the same character - how do I handle duplicates and scoring, and how do I set up the relationship between the cards and decks tables?

> it seems intuitive for each user to only have one (for example) 是 in their list, even if the character may have multiple meanings and/or pronunciations - as the app tests recognition, not the depth of knowledge about each character

* 1 user <-> m cards // 1-m

> \* I cannot have unique cards used by multiple users (1-m), because the users need to have editing control over their vocabulary, which is also what an evolving character difficulty score/rating practically is 

...

> the user may organize their cards into different decks

* 1 user <-> m decks // 1-m

...

> the above means that the same character may be found in different decks, -AND- may have different meanings and/or pronunciations there, making them essentialy new cards of the same character - therefore, there both needs to be duplicates of characters for the purpose of storing decks -AND- NOT be duplicates in the users' overall list (but this is a point of consideration for code, not table structure)

* 1 card <-> m decks // 1-m

> \* I cannot have unique decks because a deck here is not handled as a unique entity (such as a card or a user), but is rather a label for a card-deck combo

...

### My solution:


