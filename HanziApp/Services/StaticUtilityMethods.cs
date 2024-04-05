using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using Models.Database_Entities;

//static classes cannot have instance constructors
// methods here serve as utility, they do not call methods from outside this class or query the database

// *****************************************************
//
// - COMPARE / RETURN BOOL
// CheckIfThisCharacterAlreadyExists()
// --> DoTheseThreeMatch()
// --> DoTheCharactersMatch()
// --> DoCharactersAndPinyinMatch()
// FindSameValueBetweenStrings()
//
// - PROCESS LISTS
// RemoveDuplicatesFromExcelListIfFound()
// ReturnFlashcardsFromFirstListThatMatchWithFlashcardsInSecondList()
// ^ SeparateIncomingFlashcardsBetweenBrandNewAndNot()
// ^ FindDuplicateCharactersOfIncomingListInExistingList()
// FilterOutAndReturnOnlyDistinctByIdFromFirstList()
// SeparateOneListOfDuplicatesIntoTwoByTimeAdded()
// FilterOutExistingTernaryTablePrimaryKeyCombinationsFromTwoLists()
//
// - CONVERT TYPES
// ConvertListOfFlashcardsAndDecksViewEntityToDeckEntityList()
// ConvertDeckEntityToFlashcardsAndDecksViewEntity()
// ConvertListOfFlashcardEntityToFlashcardsAndDecksViewEntity()
// ConvertListOfFlashcardsAndDecksViewEntityToFlashcardEntity()
// ConvertFlashcardsAndDecksViewEntityToFlashcardEntity()
//
// *****************************************************

namespace Services
{
    public static class StaticUtilityMethods
    {
        // ---------------- COMPARE / RETURN BOOL ------------------

        // CHECK FOR EXISTING ENTRY TO AVOID ADDING DUPLICATES WHEN MANUALLY ADDING NEW FLASHCARD
        public static bool CheckIfThisCharacterAlreadyExists(FlashcardsAndDecksViewEntity model, List<FlashcardEntity> entireList)
        {
            bool foundMatch = entireList.Any(list => //all three not null/empty and match check
                                                    DoTheseThreeMatch(list.Simplified, list.Traditional, list.Pinyin, model.Simplified, model.Traditional, model.Pinyin)
                                                    || // OR simplified and traditional match, but pinyin is different
                                                    DoTheCharactersMatch(list.Simplified, list.Traditional, model.Simplified, model.Traditional)
                                                    || // OR simplified is different or null/empty, so only check pinyin and traditional for matches
                                                    DoCharactersAndPinyinMatch(list.Traditional, list.Pinyin, model.Traditional, model.Pinyin)
                                                    || // OR traditional is different or null/empty, so only check pinyin and simplified for matches
                                                    DoCharactersAndPinyinMatch(list.Simplified, list.Pinyin, model.Simplified, model.Pinyin)
                                            ); // it is NOT possible to make matches if one side has simplified but no trad, and another has traditional but no simp, even if pinyin is identical ... because of how Chinese works
            return foundMatch;
        }

        // does Simplified+Traditional+Pinying match
        public static bool DoTheseThreeMatch(string s1, string t1, string p1, string s2, string t2, string p2)
        {
            bool result = (
                            !string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2) && s2.Trim() == s1.Trim()
                            &&
                            !string.IsNullOrEmpty(t1) && !string.IsNullOrEmpty(t2) && t2.Trim() == t1.Trim()
                            &&
                            !string.IsNullOrEmpty(p1) && !string.IsNullOrEmpty(p2) && p2.Replace(" ", "").ToLower() == p1.Replace(" ", "").ToLower()
                          );
            return result;
        }

        // does Simplified+Traditional match
        public static bool DoTheCharactersMatch(string s1, string t1, string s2, string t2)
        {
            bool result = (
                            !string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2) && s2.Trim() == s1.Trim()
                            &&
                            !string.IsNullOrEmpty(t1) && !string.IsNullOrEmpty(t2) && t2.Trim() == t1.Trim()
                          );
            return result;
        }

        // does Simplified+Pinyin match
        // does Traditional+Pinyin match
        public static bool DoCharactersAndPinyinMatch(string c1, string p1, string c2, string p2)
        {
            bool result = (
                            !string.IsNullOrEmpty(c1) && !string.IsNullOrEmpty(c2) && c2.Trim() == c1.Trim()
                            &&
                            !string.IsNullOrEmpty(p1) && !string.IsNullOrEmpty(p2) && p2.Replace(" ", "").ToLower() == p1.Replace(" ", "").ToLower()
                          );
            return result;
        }

        // COMPARE SINGLE FLASHCARD STRING PROPERTIES        
        public static bool FindSameValueBetweenStrings(string a, string b)
        {
            bool result = (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) // Both are empty or null
                            ||
                            (
                                !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) // Both aren't null
                                &&
                                (
                                    ( // found in parts between "|"
                                        b.Split('|').Any(part => part.Trim().Equals(a.Trim(), StringComparison.OrdinalIgnoreCase))
                                        ||
                                        a.Split('|').Any(part => part.Trim().Equals(b.Trim(), StringComparison.OrdinalIgnoreCase))
                                    )
                                    ||
                                    ( // found in its entirety
                                        a.Trim().Contains(b.Trim(), StringComparison.OrdinalIgnoreCase)
                                        ||
                                        b.Trim().Contains(a.Trim(), StringComparison.OrdinalIgnoreCase)
                                    )
                                )
                            );
            return result; // finds "learn" in "to study | learn", meaning fewer bad appendings happening
        }


        // ---------------- PROCESS LISTS ------------------

        // CHECKING FOR DUPLICATES IN LIST BEFORE UPLOAD, REMOVES DUPLICATES
        public static (List<FlashcardEntity> noDuplicates, List<FlashcardEntity> duplicates) RemoveDuplicatesFromExcelListIfFound(List<FlashcardEntity> list)
        {
            List<FlashcardEntity> duplicatePairs = list
                .GroupBy(x => new
                {
                    Simplified = x.Simplified?.Trim()?.ToUpper(),
                    Traditional = x.Traditional?.Trim()?.ToUpper(),
                    Pinyin = x.Pinyin?.Trim()?.ToUpper(),
                    Translation = x.Translation?.Trim()?.ToUpper()
                })
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.Skip(1))
                .ToList();

            //Console.WriteLine(duplicatePairs);

            List<FlashcardEntity> noDuplicates = list
                .GroupBy(x => new
                {
                    Simplified = x.Simplified?.Trim()?.ToUpper(),
                    Traditional = x.Traditional?.Trim()?.ToUpper(),
                    Pinyin = x.Pinyin?.Trim()?.ToUpper(),
                    Translation = x.Translation?.Trim()?.ToUpper()
                })
                .Select(group => group.First())
                .ToList();

            //Console.WriteLine(noDuplicates);

            return (noDuplicates, duplicatePairs);
        }

        // RETURN FLASHCARDS THAT MATCH BETWEEN TWO LISTS
        public static List<FlashcardEntity> ReturnFlashcardsFromFirstListThatMatchWithFlashcardsInSecondList(List<FlashcardEntity> firstList, List<FlashcardEntity> secondList)
        {
            List<FlashcardEntity> flashcardsThatMatch = firstList.Where(first =>
                secondList.Any(second => //all three not null/empty and match check
                                                    DoTheseThreeMatch(second.Simplified, second.Traditional, second.Pinyin, first.Simplified, first.Traditional, first.Pinyin)
                                                    || // OR simplified and traditional match, but pinyin is different
                                                    DoTheCharactersMatch(second.Simplified, second.Traditional, first.Simplified, first.Traditional)
                                                    || // OR simplified is different or null/empty, so only check pinyin and traditional for matches
                                                    DoCharactersAndPinyinMatch(second.Traditional, second.Pinyin, first.Traditional, first.Pinyin)
                                                    || // OR traditional is different or null/empty, so only check pinyin and simplified for matches
                                                    DoCharactersAndPinyinMatch(second.Simplified, second.Pinyin, first.Simplified, first.Pinyin)
                                            ) // it is NOT possible to make matches if one side has simplified but no trad, and another has traditional but no simp, even if pinyin is identical ... because of how Chinese works
            ).ToList();

            return flashcardsThatMatch;
        }

        // SEPARATE THE INCOMING LIST INTO BRAND NEW FLASHCARDS AND FLASHCARDS WHICH ONLY ADD NEW INFO TO EXISTING FOUND DUPLICATES
        public static (List<FlashcardEntity> brandNewFlashcards, List<FlashcardEntity> newInfoForOldFlashcards) SeparateIncomingFlashcardsBetweenBrandNewAndNot(List<FlashcardEntity> incomingList, List<FlashcardEntity> existingDuplicates)
        {
            List<FlashcardEntity> newInfoForOldFlashcards = ReturnFlashcardsFromFirstListThatMatchWithFlashcardsInSecondList(incomingList, existingDuplicates);
            List<FlashcardEntity> brandNewFLashcards = incomingList.Except(newInfoForOldFlashcards).ToList();

            return (brandNewFLashcards, newInfoForOldFlashcards);
        }

        // FINDS CHARACTERS FROM THE INCOMING LIST WHICH ALREADY EXIST IN THE USER'S VOCABULARY, RETURN THESE EXISTING DUPLICATES
        public static List<FlashcardEntity> FindDuplicateCharactersOfIncomingListInExistingList(List<FlashcardEntity> incomingList, List<FlashcardEntity> oldList)
        {
            List<FlashcardEntity> oldFlashcardDuplicates = ReturnFlashcardsFromFirstListThatMatchWithFlashcardsInSecondList(oldList, incomingList);

            return oldFlashcardDuplicates;
        }
                
        // RETURN ONLY DISTINCT BY ID FROM FIRST LIST
        public static List<GenericModel> FilterOutAndReturnOnlyDistinctByIdFromFirstList<GenericModel>(List<GenericModel> firstList, List<GenericModel> secondList)
        {
            List<GenericModel> returnList = firstList.Where(x => !secondList.Any(y => (y as dynamic).Id == (x as dynamic).Id))
                                                    .GroupBy(x => (x as dynamic).Id)
                                                    .Select(group => group.First())
                                                    .ToList();
            return returnList;
        }

        // SEPARATE LIST OF FLASHCARD DUPLICATES TO A LIST OF ORIGINALS AND A LIST OF NEWER ONES
        // for cleaning up duplicates at the end of adding list
        public static (List<FlashcardsAndDecksViewEntity> olderEntries, List<FlashcardsAndDecksViewEntity> newerEntries) SeparateOneListOfDuplicatesIntoTwoByTimeAdded(List<FlashcardsAndDecksViewEntity> listOfDuplicates)
        {
            List<FlashcardsAndDecksViewEntity> olderEntries = new List<FlashcardsAndDecksViewEntity>();
            List<FlashcardsAndDecksViewEntity> newerEntries = new List<FlashcardsAndDecksViewEntity>();
            FlashcardsAndDecksViewEntity previous = null;

            foreach (FlashcardsAndDecksViewEntity flashcard in listOfDuplicates)
            {

                if (previous != flashcard && previous != null)
                {
                    bool simplifiedNotNull = !string.IsNullOrEmpty(previous.Simplified) && !string.IsNullOrEmpty(flashcard.Simplified);

                    bool simplifiedIdentical = false;
                    if (simplifiedNotNull)
                    {
                        simplifiedIdentical = previous.Simplified.Trim() == flashcard.Simplified.Trim();
                    }

                    bool traditionalNotNull = !string.IsNullOrEmpty(previous.Traditional) && !string.IsNullOrEmpty(flashcard.Traditional);

                    bool traditionalIdentical = false;
                    if (traditionalNotNull)
                    {
                        traditionalIdentical = previous.Traditional.Trim() == flashcard.Traditional.Trim();
                    }

                    // first check for polyphones
                    // if both Simplified and Traditional field exists, and Simplified is identical but Traditional is not, skip
                    if (simplifiedNotNull && traditionalNotNull)
                    {
                        bool simplifiedIsSameButTraditionalIsDifferent = (previous.Simplified.Trim() == flashcard.Simplified.Trim()) && (previous.Traditional.Trim() != flashcard.Traditional.Trim());
                        if (simplifiedIsSameButTraditionalIsDifferent)
                        {
                            //Console.WriteLine($"One is {previous.Translation}, {previous.Pinyin} and other is {flashcard.Translation}, {flashcard.Pinyin} \n");

                            previous = flashcard;
                            continue;
                        }
                    }

                    if ((simplifiedNotNull && simplifiedIdentical) || (traditionalNotNull && traditionalIdentical))
                    {
                        if (flashcard.FlashcardTimeAdded < previous.FlashcardTimeAdded)
                        {
                            olderEntries.Add(flashcard);
                            newerEntries.Add(previous);
                            //Console.WriteLine($"Newer is {previous.Simplified}, {previous.Pinyin} and Older is {flashcard.Simplified}, {flashcard.Pinyin} \n");

                        }
                        else
                        {
                            olderEntries.Add(previous);
                            newerEntries.Add(flashcard);
                            //Console.WriteLine($"Older is {previous.Simplified}, {previous.Pinyin} and Newer is {flashcard.Simplified}, {flashcard.Pinyin} \n");
                        }
                    }
                }
                previous = flashcard;
            }

            return (olderEntries, newerEntries);
        }

        // COMPARES TWO TERNARY TABLE LISTS TO RETURN ONLY NEW COMBINATIONS, SAFE TO SAVE
        public static List<CardDeckUserTernaryTableEntity> FilterOutExistingTernaryTablePrimaryKeyCombinationsFromTwoLists(List<CardDeckUserTernaryTableEntity> listToFilter, List<CardDeckUserTernaryTableEntity> listToKeep)
        {
            if (listToKeep == null || listToKeep.Count() == 0)
            {
                return listToFilter;
            }

            List<CardDeckUserTernaryTableEntity> newCombinations = listToFilter.Where( // where itemBeingChecked has NO (!) matches in listToKeep per criteria
                                                                                itemBeingChecked => !listToKeep.Any(
                                                                                    itemStaying => itemBeingChecked.FlashcardId == itemStaying.FlashcardId
                                                                                                    &&
                                                                                                    itemBeingChecked.UserId == itemStaying.UserId
                                                                                                        )
                                                                                    ).ToList(); // skipping deckId because we are comparing two deck entries duuuh, they'll never match 
            return newCombinations;
        }


        // ---------------- CONVERT TYPES ------------------

        // CONVERT LISTS - FlashCardsAndDecksViewEntity TO DeckEntity FOR DROPDOWN MENU
        public static List<DeckEntity> ConvertListOfFlashcardsAndDecksViewEntityToDeckEntityList(List<FlashcardsAndDecksViewEntity> wholeList)
        {
            List<DeckEntity> deckData = wholeList.Select(viewModel => new DeckEntity
            {
                DeckName = viewModel.DeckName,
                BookName = viewModel.BookName,
                UnitName = viewModel.UnitName,
            }).ToList();

            return deckData;
        }

        // CONVERT DeckEntity TO FlashcardsAndDecksViewEntity
        public static FlashcardsAndDecksViewEntity ConvertDeckEntityToFlashcardsAndDecksViewEntity(DeckEntity deck)
        {
            FlashcardsAndDecksViewEntity viewEntity = new FlashcardsAndDecksViewEntity
            {
                DeckId = deck.Id,
                DeckName = deck.DeckName,
                BookName = deck.BookName,
                UnitName = deck.UnitName,
            };

            return viewEntity;
        }

        // CONVERT LIST FROM FLASHCARD ENTITY TO VIEW ENTITY
        public static List<FlashcardsAndDecksViewEntity> ConvertListOfFlashcardEntityToFlashcardsAndDecksViewEntity(List<FlashcardEntity> listToConvertToViewEntity)
        {
            List<FlashcardsAndDecksViewEntity> list = listToConvertToViewEntity.Select(flashcard => new FlashcardsAndDecksViewEntity
            {
                Id = flashcard.Id,
                Simplified = flashcard.Simplified,
                Traditional = flashcard.Traditional,
                Pinyin = flashcard.Pinyin,
                Translation = flashcard.Translation,
                Note = flashcard.Note,
                FlashcardTimeUpdated = flashcard.TimeUpdated,
                FlashcardTimeAdded = flashcard.TimeAdded,
                DifficultyRating = flashcard.DifficultyRating
            }).ToList();

            return list;
        }

        // CONVERT LIST FROM VIEW ENTITY BACK TO FLASHCARD ENTITY
        public static List<FlashcardEntity> ConvertListOfFlashcardsAndDecksViewEntityToFlashcardEntity(List<FlashcardsAndDecksViewEntity> listToConvertBack)
        {
            List<FlashcardEntity> list = listToConvertBack.Select(flashcard => new FlashcardEntity
            {
                Id = flashcard.Id,
                Simplified = flashcard.Simplified,
                Traditional = flashcard.Traditional,
                Pinyin = flashcard.Pinyin,
                Translation = flashcard.Translation,
                Note = flashcard.Note,
                TimeUpdated = flashcard.FlashcardTimeUpdated,
                TimeAdded = flashcard.FlashcardTimeAdded,
                DifficultyRating = flashcard.DifficultyRating
            }).ToList();

            return list;
        }

        // CONVERT FROM VIEW ENTITY BACK TO FLASHCARD ENTITY
        public static FlashcardEntity ConvertFlashcardsAndDecksViewEntityToFlashcardEntity(FlashcardsAndDecksViewEntity model)
        {
            FlashcardEntity flashcard = new FlashcardEntity
            {
                Id = model.Id,
                Simplified = model.Simplified,
                Traditional = model.Traditional,
                Pinyin = model.Pinyin,
                Translation = model.Translation,
                Note = model.Note,
                TimeUpdated = model.FlashcardTimeUpdated,
                TimeAdded = model.FlashcardTimeAdded,
                DifficultyRating = model.DifficultyRating
            };

            return flashcard;
        }

    }
}
