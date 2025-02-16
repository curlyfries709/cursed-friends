using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Linq;

[System.Serializable]
public struct GameDate
{
    public GameDate(int day, int month, int year, Period period)
    {
        this.day = day;
        this.month = month;
        this.year = year;
        this.period = period;
    }

    public int day;
    public int month;
    public int year;
    public Period period;
}

public enum Period
{
    Morning,
    BreakTime,
    AfterBreak,
    LunchTime,
    Afternoon, //A.K.A AfterLunch
    Afterschool,
    Evening,
    NightTime
}
public class CalendarManager : MonoBehaviour
{
    public static CalendarManager Instance;

    [Header("Start Date")]
    [SerializeField] int startDay = 07;
    [SerializeField] int startMonth = 09;
    [SerializeField] int startYear = 2021;
    [Space(10)]
    [SerializeField] Period startPeriod;
    [Header("Days & Periods")]
    [SerializeField] List<DayData> daysData;
    [Space(10)]
    [SerializeField] List<PeriodData> dayOffData;
    [Header("Days Off")]
    [SerializeField] List<GameDateRange> scheduledDaysOff = new List<GameDateRange>();    

    //Cache
    public DateTime currentDate { get; private set; }
    public Period currentPeriod { get; private set; }

    Dictionary<DayOfWeek, List<PeriodData>> daysDataDict = new Dictionary<DayOfWeek, List<PeriodData>>();
    List<CalendarEvent> calendarEvents = new List<CalendarEvent>();

    //Events
    public static Action AdvanceTime;
    public static Action<GameDate> BeginNewDay;

    public static Func<GameDate, CalendarEvent> BeginNewPeriod;

    [System.Serializable]
    public struct DayData
    {
        public DayOfWeek day;
        public List<PeriodData> orderedPeriodsForDay;
    }

    [System.Serializable]
    public struct PeriodData
    {
        public Period period;
        public bool isFreePeriod; //Free periods are periods when player gets contol to complete activities. 
    }

    [System.Serializable]
    public struct GameDateRange
    {
        public GameDate startDate;
        public GameDate endDate;
    }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

        AdvanceTime += OnAdvanceTime;
    }

    void Start()
    {
        NewGameSetup();
        PopulateDayDataDict();
    }

    private void OnAdvanceTime()
    {
        if (TriggerCalendarEvent())
        {
            return;
        }

        DayOfWeek dayOfWeek = GetCurrentDayOfWeek();
        List<PeriodData> currentDayPeriods = daysDataDict[dayOfWeek];
        bool isNewPeriodOnDayOff = false;

        //Check if it is DayOff
        if (IsDayOff(currentDate))
        {
            currentDayPeriods = dayOffData;
            isNewPeriodOnDayOff = true;
        }

        PeriodData currentPeriodData = currentDayPeriods.Where((data) => data.period == currentPeriod).FirstOrDefault();
        int indexOfNextPeriodData = currentDayPeriods.IndexOf(currentPeriodData) + 1;

        if(indexOfNextPeriodData >= currentDayPeriods.Count)
        {
            //Current period is final period of day. Go to next day
            currentDate = currentDate.AddDays(1);
            PeriodData updatedPeriodData = daysDataDict[GetCurrentDayOfWeek()][0];

            isNewPeriodOnDayOff = IsDayOff(currentDate);

            //Check if new day is DayOff
            if (isNewPeriodOnDayOff)
            {
                updatedPeriodData = dayOffData[0];
            }

            currentPeriod = updatedPeriodData.period;

            //Trigger Systems Event
            BeginNewDay?.Invoke(GetCurrentGameDate());
        }
        else
        {
            //Move to Next Period
            currentPeriod = currentDayPeriods[indexOfNextPeriodData].period;
        }

        if (BeginNewPeriod != null) //Check if something subscribed to NewPeriod Event.
        {
            //Get New Calendar Events for new period.
            foreach (Func<GameDate, CalendarEvent> listener in BeginNewPeriod.GetInvocationList())
            {
                CalendarEvent calendarEvent = listener.Invoke(GetCurrentGameDate());

                if (calendarEvent && !calendarEvents.Contains(calendarEvent))
                {
                    calendarEvents.Add(calendarEvent);
                }
            }

            //Order the queue based on priority
            calendarEvents = calendarEvents.OrderBy((calendarEvent) => calendarEvent.GetPriority()).ToList();
        }

        //Trigger New Event
        if (TriggerCalendarEvent())
        {
            return;
        }

        PeriodData newPeriodData = (isNewPeriodOnDayOff ? dayOffData : daysDataDict[GetCurrentDayOfWeek()]).Where((data) => data.period == currentPeriod).FirstOrDefault();

        Debug.Log("DAY: " + GetCurrentDayOfWeek().ToString() + " DATE: " + currentDate.ToString() + " Period: " + 
            currentPeriod.ToString() + " Is Day Off: " + isNewPeriodOnDayOff.ToString());

        if (newPeriodData.isFreePeriod)
        {
            BeginFreePeriod();
        }
        else
        {
            //Advance time again, this is probably a secret period (A period unknown to the player but built into the game system for triggering events)
            AdvanceTime?.Invoke();
        }
    }

    private void BeginFreePeriod()
    {
        //Give Player Control. 
    }

    private bool TriggerCalendarEvent()
    {
        bool trigger = calendarEvents.Count > 0;

        if (trigger)
        {
            calendarEvents[0].TriggerEvent();
            calendarEvents.RemoveAt(0);  
        }

        return trigger;
    }

    private void OnDisable()
    {
        AdvanceTime -= OnAdvanceTime;
    }


    private void NewGameSetup()
    {
        currentDate = new DateTime(startYear, startMonth, startDay);
        currentPeriod = startPeriod;
    }

    private void PopulateDayDataDict()
    {
        foreach (DayData data in daysData)
        {
            daysDataDict[data.day] = data.orderedPeriodsForDay;
        }
    }

    //GETTERS


    public GameDate GetCurrentGameDate()
    {
        return new GameDate(currentDate.Day, currentDate.Month, currentDate.Year, currentPeriod);
    }

    public int GetDaysPassed(DateTime daysPassedSinceDate)
    {
        return (currentDate - daysPassedSinceDate).Days;
    }

    public bool IsDayOff(GameDate gameDateToCheck)
    {
        return IsDayOff(GetGameDateAsDateTime(gameDateToCheck));
    }

    public bool IsDayOff(DateTime dateToCheck)
    {
        foreach (GameDateRange range in scheduledDaysOff)
        {
            if (IsDateInRange(dateToCheck, range))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDateInRange(DateTime dateToCheck, GameDateRange gameDateRange)
    {
        return dateToCheck >= GetGameDateAsDateTime(gameDateRange.startDate) && dateToCheck <= GetGameDateAsDateTime(gameDateRange.endDate);
    }

    public bool IsDateInRange(GameDate gameDateToCheck, GameDateRange gameDateRange)
    {
        DateTime dateToCheck = GetGameDateAsDateTime(gameDateToCheck);
        return IsDateInRange(dateToCheck, gameDateRange);
    }

    public string GetCurrentDayMonthInGameFormat()
    {
        string date = currentDate.Day.ToString() + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(currentDate.Month);
        return date;
    }


    public DateTime GetGameDateAsDateTime(GameDate gameDate)
    {
        return new DateTime(gameDate.year, gameDate.month, gameDate.day);
    }

    public DayOfWeek GetCurrentDayOfWeek()
    {
        return currentDate.DayOfWeek;
    }
}
