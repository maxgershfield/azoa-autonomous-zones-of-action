// SPDX-License-Identifier: Apache-2.0

using AZOA.WebAPI.Models.FieldGuide;

namespace AZOA.WebAPI.Services.FieldGuide;

public interface IFieldGuideGeoPinStore
{
    FieldGuideGeoPin Place(FieldGuidePlacePinRequest request, Guid? createdByUserId);
    IReadOnlyList<FieldGuideGeoPin> Nearby(double latitude, double longitude, double radiusKm);
    void RecordStreetEvent(FieldGuideStreetEventRequest request);
}
