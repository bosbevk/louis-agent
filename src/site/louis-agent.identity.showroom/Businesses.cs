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
/// Graanveld Family Practice, a made-up doctor's practice: it answers the practice's hours and services and passes an
/// appointment request on, never gives medical advice, and sends an emergency to the emergency number.
/// </summary>
public sealed class DoctorsPracticeIdentity() : ShowroomIdentity(ShowroomIdentityName.GraanveldFamilyPractice);
