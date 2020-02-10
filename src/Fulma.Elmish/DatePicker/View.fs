namespace Fulma.Elmish.DatePicker

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Extensions.Wikiki
open Types
open System

module View =
    let isCalendarDisplayed state =
        state.InputFocused && not (state.AutoClose && state.ForceClose)

    let onFocus (config : Config<'Msg>) state currentDate dispatch =
        // If the calendar is already displayed don't dispatch a new onFocus message
        // This is needed because we register to both onClick and onFocus event
        if not(isCalendarDisplayed state) then
            config.OnChange
                ({ state with InputFocused = true
                              ForceClose = false }, currentDate)
                |> dispatch

    let onChange (config : Config<'Msg>) state currentDate dispatch =
        config.OnChange
            (state, currentDate)
            |> dispatch

    let onDeleteClick (config : Config<'Msg>) state (currentDate : DateTime option) dispatch =
        if currentDate.IsSome then config.OnChange (state, None) |> dispatch

    let calendar (config : Config<'Msg>) state (currentDate : DateTime option) dispatch =
        let isCurrentMonth (date : DateTime) =
            state.ReferenceDate.Month  = date.Month

        let isToday (dateToCompare : DateTime) =
            let d = DateTime.UtcNow
            dateToCompare.Day = d.Day && dateToCompare.Month = d.Month && dateToCompare.Year = d.Year

        let isSelected (dateToCompare : DateTime) =
            match currentDate with
            | Some date -> date.Day = dateToCompare.Day && date.Month = dateToCompare.Month && date.Year = dateToCompare.Year
            | None -> false

        let firstDateCalendar =
            let firstOfMonth = DateTime(state.ReferenceDate.Year, state.ReferenceDate.Month, 1)
            let weekOffset =
                (7 + (int firstOfMonth.DayOfWeek) - (int config.Local.Date.FirstDayOfTheWeek))
                    % 7
            firstOfMonth.AddDays(float -weekOffset)

        let header =
            [0..6]
            |> List.splitAt (int firstDateCalendar.DayOfWeek)
            |> (fun (first, second) -> second @ first)
            |> List.map (fun intDayOfWeek ->
                let dayOfWeek = enum<System.DayOfWeek> intDayOfWeek
                let name =
                    match dayOfWeek with
                    | DayOfWeek.Monday    -> config.Local.Date.AbbreviatedDays.Monday
                    | DayOfWeek.Tuesday   -> config.Local.Date.AbbreviatedDays.Tuesday
                    | DayOfWeek.Wednesday -> config.Local.Date.AbbreviatedDays.Wednesday
                    | DayOfWeek.Thursday  -> config.Local.Date.AbbreviatedDays.Thursday
                    | DayOfWeek.Friday    -> config.Local.Date.AbbreviatedDays.Friday
                    | DayOfWeek.Saturday  -> config.Local.Date.AbbreviatedDays.Saturday
                    | DayOfWeek.Sunday    -> config.Local.Date.AbbreviatedDays.Sunday
                    | x -> failwithf "not a valid day of week: %A" x
                Calendar.Date.date [ ] [ str name ])

        let body =
            seq {
                for dayRank = 0 to 41 do // We have 42 dates to show
                    let date = firstDateCalendar.AddDays(float dayRank)
                    yield Calendar.Date.date [ Calendar.Date.IsDisabled (not (isCurrentMonth date)) ]
                                [ Calendar.Date.item [ Calendar.Date.Item.IsToday (isToday date)
                                                       Calendar.Date.Item.IsActive (isSelected date)
                                                       Calendar.Date.Item.Props [ OnClick (fun _ ->
                                                                                                let newState = { state with ForceClose = true }
                                                                                                onChange config newState (Some date) dispatch) ] ]
                                    [ str (date.Day.ToString()) ] ]
            } |> Seq.toList

        Box.box' [ Common.Props [ Style config.DatePickerStyle ] ]
                 [ Calendar.calendar [ Calendar.Props [ OnMouseDown (fun ev -> ev.preventDefault()) ]
                                                   ]
                                     [ Calendar.Nav.nav [ ]
                                         [ Calendar.Nav.left [ ]
                                             [ Button.button [ Button.IsLink
                                                               Button.OnClick (fun _ -> let newState = { state with ReferenceDate = state.ReferenceDate.AddMonths(-1)
                                                                                                                    ForceClose = false }
                                                                                        onChange config newState currentDate dispatch) ]
                                                             [ Icon.icon [ ]
                                                                [ Fa.i [ Fa.Solid.ChevronLeft ]
                                                                    [ ] ] ] ]
                                           str (Date.Format.localFormat config.Local "MMMM yyyy" state.ReferenceDate)
                                           Calendar.Nav.right [ ]
                                             [ Button.button [ Button.IsLink
                                                               Button.OnClick (fun _ -> let newState = { state with ReferenceDate = state.ReferenceDate.AddMonths(1)
                                                                                                                    ForceClose = false }
                                                                                        onChange config newState currentDate dispatch) ]
                                                             [ Icon.icon [ ]
                                                                [ Fa.i [ Fa.Solid.ChevronRight ]
                                                                    [ ] ] ] ] ]
                                       div [ ]
                                           [ Calendar.header [ ] header
                                             Calendar.body [ ] body ] ] ]


    let root<'Msg> (config: Config<'Msg>) (state: State) (currentDate: DateTime option) dispatch =
        let dateTxt =
            match currentDate with
            | Some date ->
                Date.Format.localFormat config.Local config.Local.Date.DefaultFormat date
            | None -> ""
        div [ ]
            [
              yield Field.body [] [
                  Field.div (if state.ShowDeleteButton then [Field.HasAddons] else []) [
                    yield Control.p [ Control.IsExpanded ] [
                            Input.text [ Input.Props [ Value dateTxt
                                                       OnFocus (fun _ -> onFocus config state currentDate dispatch)
                                                       OnClick (fun _ -> onFocus config state currentDate dispatch)
                                                       // TODO: Implement something to trigger onChange only if the value actually change
                                                       OnBlur (fun _ -> let newState = { state with InputFocused = false }
                                                                        onChange config newState currentDate dispatch) ]; ] ]

                    if state.ShowDeleteButton then
                        yield Control.p [] [
                                Button.a [ Button.OnClick(fun _ -> onDeleteClick config state currentDate dispatch) ]
                                    [ Icon.icon [ ]
                                        [ Fa.i [ Fa.Solid.Times ]
                                            [ ] ] ] ]
                  ]
              ]

              if isCalendarDisplayed state then
                yield calendar config state currentDate dispatch ]
