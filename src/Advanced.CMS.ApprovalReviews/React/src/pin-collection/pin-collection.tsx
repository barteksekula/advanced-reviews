import "./pin-collection.scss";

import { inject, observer } from "mobx-react";
import React from "react";

import Pin from "../pin/pin";
import createPositionCalculator from "../position-calculator/position-calculator";
import { IReviewComponentStore, PinLocation } from "../store/review-store";

interface PinCollectionProps {
    reviewStore?: IReviewComponentStore;
    newLocation?: PinLocation;
    positionCalculator?: ReturnType<typeof createPositionCalculator>;
}

const PinCollection: React.FC<PinCollectionProps> = ({ reviewStore, newLocation, positionCalculator }) => {
    const onLocationClick = (e, location: PinLocation) => {
        e.stopPropagation();
        reviewStore.selectedPinLocation = reviewStore.editedPinLocation = location;
    };

    const { selectedPinLocation, filteredReviewLocations } = reviewStore!;
    const locations = [...filteredReviewLocations];

    if (newLocation && !locations.some((location) => location === newLocation)) {
        locations.push(newLocation);
    }

    return (
        <div>
            {locations.map((location) => (
                <Pin
                    key={location.id || "unsaved"}
                    location={location}
                    position={positionCalculator.calculate(location)}
                    showDialog={(e) => onLocationClick(e, location)}
                    highlighted={location === selectedPinLocation}
                />
            ))}
        </div>
    );
};

export default inject("reviewStore")(observer(PinCollection));
