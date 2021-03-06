﻿namespace Attoparsec.Tests

open NUnit.Framework
open FsUnit
open FsCheck
open FsCheck.NUnit
open System
open Attoparsec
open Attoparsec.Parser
open Attoparsec.String

[<TestFixture>]
module StringParserTest =

  let cons (w: char) s = (string w) + s

  [<Test>]
  let ``satisfy`` () =
    check <| fun w s ->
      let actual =
        cons w s
        |> parse (satisfy (fun x -> x <= w))
        |> ParseResult.option
      actual = Some w

  [<Test>]
  let ``char `` () =
    check <| fun w s ->
      let actual =
        cons w s
        |> parse (pchar w)
        |> ParseResult.option
      actual = Some w

  [<Test>]
  let ``anyChar`` () =
    check <| fun s ->
      let p = (parse anyChar s).Option
      match List.ofSeq s with | [] -> p = None | x::_ -> p = Some x

  [<Test>]
  let ``notChar`` () =
    check <| fun w s ->
      (not <| String.IsNullOrEmpty(s)) ==>
        lazy (let v = s.Chars(0) in (parse (notChar w) s).Option = (if v = w then None else Some v))

  [<Test>]
  let ``string `` () =
    check <| fun s t ->
      (parse (pstring s) (s + t)).Option
      |> Option.map BmpString.toString = Some s

  [<Test>]
  let ``takeCount`` () =
    check <| fun k s ->
      (k >= 0) ==> lazy (match (parse (take k) s).Option with | None -> k > String.length s | Some _ -> k <= String.length s)

  [<Test>]
  let ``takeWhile`` () =
    check <| fun w s ->
      let (h, t) = BmpString.span ((=) w) (BmpString.ofString s)
      s
      |> parseOnly (parser {
        let! hp = takeWhile ((=) w)
        let! tp = takeText
        return (hp, tp)
      })
      |> (=) (Choice1Of2 (h, t))

  [<Test>]
  let ``takeWhile1`` () =
    check <| fun w s ->
      let sp = BmpString.cons w (BmpString.ofString s)
      let (h, t) = BmpString.span (fun x -> x <= w) sp
      sp
      |> BmpString.toString
      |> parseOnly (parser {
        let! hp = takeWhile1 (fun x -> x <= w)
        let! tp = takeText
        return (hp, tp) })
      |> (=) (Choice1Of2 (h, t))

  [<Test>]
  let ``takeWhile1 empty`` () =
    ""
    |> parse (Attoparsec.String.takeWhile1 (fun _ -> true))
    |> ParseResult.option
    |> should equal None

  [<Test>]
  let ``endOfInput`` () =
    check <| fun s ->
      s |> parseOnly endOfInput = (if String.IsNullOrEmpty s then Choice1Of2 () else Choice2Of2 "endOfInput")

  [<Test>]
  let ``match_`` () =
    check <| fun (s: int) ->
      let input =  string s
      let expected = (input, s)
      input
      |> parseOnly (match_ signedInt |>> (fun (x, y) -> (BmpString.toString x, int y)))
      |> (=) (Choice1Of2 expected)

  let signum =
    (pchar '+' |>> fun _ -> 1)
    <|> (pchar '-' |>> fun _ -> -1)
    <|> ok 1

  [<Test>]
  let ``signum `` () =
    check <| fun (s: string) ->
      let bs = BmpString.ofString s
      ((s.StartsWith("-") || s.StartsWith("+")) |> not) ==>
        (match parse signum ("+" + s) with ParseResult.Done(s, 1) when bs = s -> true | _ -> false
        && match parse signum ("-" + s) with ParseResult.Done(s, -1) when bs = s -> true | _ -> false
        && match parse signum s |> ParseResult.feed "" with ParseResult.Done(s, 1) when bs = s -> true | _ -> false)
