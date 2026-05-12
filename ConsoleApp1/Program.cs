using System;
using System.Collections.Generic;
using System.Linq;

const int roundsToPlay = 15;
const int startingBalance = 1000;
const int betAmount = 50;

var game = new BlackjackAutoGame(startingBalance, betAmount, roundsToPlay);
game.Play();

class BlackjackAutoGame
{
    private readonly int _startingBalance;
    private readonly int _betAmount;
    private readonly int _rounds;
    private readonly Random _random = new();

    public BlackjackAutoGame(int startingBalance, int betAmount, int rounds)
    {
        _startingBalance = startingBalance;
        _betAmount = betAmount;
        _rounds = rounds;
    }

    public void Play()
    {
        Console.WriteLine("=== Blackjack Auto Game ===");
        Console.WriteLine($"Startbalans: {_startingBalance} fiches");
        Console.WriteLine($"Rondes: {_rounds}, inzet per ronde: {_betAmount}");
        Console.WriteLine();

        var player = new AutoPlayer("AutoKaart", _startingBalance, _betAmount);

        for (var round = 1; round <= _rounds; round++)
        {
            if (player.Balance < _betAmount)
            {
                Console.WriteLine($"Ronde {round}: Niet genoeg fiches om door te gaan. Speler is failliet.");
                break;
            }

            Console.WriteLine($"--- Ronde {round} ---");
            var deck = new Deck(_random);
            var dealer = new Dealer();
            var hand = new BlackjackHand();
            var dealerHand = new BlackjackHand();

            hand.AddCard(deck.Draw());
            dealerHand.AddCard(deck.Draw());
            hand.AddCard(deck.Draw());
            dealerHand.AddCard(deck.Draw());

            Console.WriteLine($"Speler start met: {hand}");
            Console.WriteLine($"Dealer toont: {dealerHand.Cards[0]}");

            var action = player.DecideAction(hand, dealerHand.Cards[0]);
            while (action == PlayerAction.Hit)
            {
                var card = deck.Draw();
                hand.AddCard(card);
                Console.WriteLine($"Speler neemt: {card} -> {hand}");
                if (hand.IsBusted)
                {
                    break;
                }
                action = player.DecideAction(hand, dealerHand.Cards[0]);
            }

            if (hand.IsBusted)
            {
                Console.WriteLine($"Speler bust met {hand.Value}. Verliest {_betAmount} fiches.");
                player.AdjustBalance(-_betAmount);
                PrintBalance(player);
                Console.WriteLine();
                continue;
            }

            Console.WriteLine($"Speler staat met {hand.Value}.");

            while (dealer.ShouldHit(dealerHand))
            {
                var card = deck.Draw();
                dealerHand.AddCard(card);
                Console.WriteLine($"Dealer neemt: {card} -> {dealerHand}");
            }

            Console.WriteLine($"Dealer eindigt met {dealerHand.Value} ({dealerHand})");
            ResolveRound(player, hand, dealerHand);
            PrintBalance(player);
            Console.WriteLine();
        }

        Console.WriteLine("=== Einde van de Auto Game ===");
        Console.WriteLine($"Eindbalans: {player.Balance} fiches");
    }

    private void ResolveRound(AutoPlayer player, BlackjackHand playerHand, BlackjackHand dealerHand)
    {
        if (dealerHand.IsBusted)
        {
            Console.WriteLine("Dealer bust! Speler wint.");
            player.AdjustBalance(_betAmount);
            return;
        }

        if (playerHand.Value > dealerHand.Value)
        {
            Console.WriteLine("Speler wint met hogere score.");
            player.AdjustBalance(_betAmount);
            return;
        }

        if (playerHand.Value < dealerHand.Value)
        {
            Console.WriteLine("Dealer wint. Speler verliest.");
            player.AdjustBalance(-_betAmount);
            return;
        }

        Console.WriteLine("Gelijkspel! Push. Geen winsten of verliezen.");
    }

    private static void PrintBalance(AutoPlayer player)
    {
        Console.WriteLine($"Huidige balans: {player.Balance} fiches");
    }
}

class AutoPlayer
{
    public string Name { get; }
    public int Balance { get; private set; }
    public int Bet { get; }

    public AutoPlayer(string name, int startingBalance, int bet)
    {
        Name = name;
        Balance = startingBalance;
        Bet = bet;
    }

    public void AdjustBalance(int amount)
    {
        Balance += amount;
    }

    public PlayerAction DecideAction(BlackjackHand hand, Card dealerVisibleCard)
    {
        if (hand.Value <= 11)
        {
            return PlayerAction.Hit;
        }

        if (hand.Value >= 17)
        {
            return PlayerAction.Stand;
        }

        if (dealerVisibleCard.Value >= 7 && hand.Value <= 16)
        {
            return PlayerAction.Hit;
        }

        return PlayerAction.Stand;
    }
}

class Dealer
{
    public bool ShouldHit(BlackjackHand hand) => hand.Value < 17;
}

enum PlayerAction
{
    Hit,
    Stand
}

class BlackjackHand
{
    public List<Card> Cards { get; } = new();

    public int Value
    {
        get
        {
            var total = Cards.Sum(c => c.Value);
            var aces = Cards.Count(c => c.Rank == Rank.Ace);
            while (total > 21 && aces > 0)
            {
                total -= 10;
                aces--;
            }
            return total;
        }
    }

    public bool IsBusted => Value > 21;

    public void AddCard(Card card) => Cards.Add(card);

    public override string ToString() => string.Join(", ", Cards.Select(c => c.ToString())) + $" (waarde {Value})";
}

class Deck
{
    private readonly List<Card> _cards;
    private int _position;

    public Deck(Random random)
    {
        _cards = Enum.GetValues<Suit>()
            .SelectMany(suit => Enum.GetValues<Rank>().Select(rank => new Card(rank, suit)))
            .OrderBy(_ => random.Next())
            .ToList();
        _position = 0;
    }

    public Card Draw()
    {
        if (_position >= _cards.Count)
        {
            throw new InvalidOperationException("Deck is leeg.");
        }

        return _cards[_position++];
    }
}

class Card
{
    public Rank Rank { get; }
    public Suit Suit { get; }
    public int Value => Rank switch
    {
        Rank.Two => 2,
        Rank.Three => 3,
        Rank.Four => 4,
        Rank.Five => 5,
        Rank.Six => 6,
        Rank.Seven => 7,
        Rank.Eight => 8,
        Rank.Nine => 9,
        Rank.Ten or Rank.Jack or Rank.Queen or Rank.King => 10,
        Rank.Ace => 11,
        _ => 0
    };

    public Card(Rank rank, Suit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    public override string ToString() => Rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)Rank).ToString()
    } + Suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?"
    };
}

enum Rank
{
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}

enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}
