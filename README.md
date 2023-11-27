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
- CAVEATS
  - each user may only have one example of a particular character in their entire list (i.e. if a user has 是, there may only be one 是), because the app is tracking how well a user recognizes a character and duplicates would result in duplicate tracking
  - each character (i.e. 是) may appear in multiple named word lists associated with a user (e.g. in the set "HSK1" and in the set "Contemporary Chinese I")
- a user profile page
  - allow users to freely change their username and password
  - track and display the easiest and hardest charcters to recognize for that user
  - overview of the user's saved vocabulary

# Challenges & Learning points

## Database planning
> How do I set up my database tables to respect the requirements outlined in the specification, notably - the CAVEATS section?
