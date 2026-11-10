namespace LouisAgent.Identities.Showroom;

/// <summary>
/// Wheatfield Kitchen, a made-up restaurant in Malmesbury: it answers diners and passes a table request on.
/// </summary>
public sealed class RestaurantIdentity() : ShowroomIdentity(ShowroomIdentityName.WheatfieldKitchen);

/// <summary>
/// Koringland Attorneys, a made-up law practice: it answers what the practice does and passes a consultation request
/// on, and never gives legal advice.
/// </summary>
public sealed class LawPracticeIdentity() : ShowroomIdentity(ShowroomIdentityName.KoringlandAttorneys);

/// <summary>
/// Seemeeu Guesthouse, a made-up guesthouse in Yzerfontein (F127.1, in place of the doctor's practice): it answers the
/// rooms, the dogs, breakfast and the beach, never promises a room it can't see, and passes a booking request on.
/// </summary>
public sealed class GuesthouseIdentity() : ShowroomIdentity(ShowroomIdentityName.SeemeeuGuesthouse);
