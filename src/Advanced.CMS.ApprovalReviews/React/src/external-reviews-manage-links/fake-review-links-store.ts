import { ExternalReviewStore, ReviewLink } from "./external-review-links-store";

function getDate(days: number) {
    const result = new Date();
    result.setDate(result.getDate() + days);
    return result;
}

function uuidv4() {
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0,
            v = c === "x" ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

export default class FakeReviewLinksStore extends ExternalReviewStore {
    constructor() {
        super(null);

        this.links = [
            new ReviewLink(
                "581888c096584a60bc51dfeab1095d62",
                null,
                "http://www.google.com",
                getDate(1).toString(),
                true,
            ),
            new ReviewLink(
                "747ae7bb415f47c0813b0052c62b8511",
                null,
                "http://www.google.com",
                getDate(-1).toString(),
                true,
            ),
            new ReviewLink(
                "193a02bda7a64672b2489171a87c72b1",
                null,
                "http://www.google.com",
                getDate(1).toString(),
                false,
            ),
            new ReviewLink(
                "39dfc3781d6148849af37bfb728dc9cf",
                null,
                "http://www.google.com",
                getDate(1).toString(),
                true,
            ),
            new ReviewLink(
                "038be60a9a9444a38954a1f114584927",
                null,
                "http://www.google.com",
                getDate(1).toString(),
                true,
            ),
            new ReviewLink(
                "774904d9720b45e9b32bc997f74fa041",
                null,
                "http://www.google.com",
                getDate(-1).toString(),
                false,
            ),
            new ReviewLink(
                "d947b5c0d14c4a5a8c6ab0c224f8817e",
                null,
                "http://www.google.com",
                getDate(2).toString(),
                true,
            ),
            new ReviewLink(
                "8e1b68777417425284e3ea2c4b9aec7e",
                null,
                "http://www.google.com",
                getDate(4).toString(),
                true,
            ),
            new ReviewLink(
                "c72b45508ace4e518a7ba9ffe0cb0d12",
                null,
                "http://www.google.com",
                getDate(10).toString(),
                false,
                null,
                "1234",
            ),
            new ReviewLink(
                "c72b45508ace4e518a7ba9ffe0cb0d12",
                null,
                "http://www.google.com",
                getDate(10).toString(),
                false,
            ),
            new ReviewLink(
                "bb08a4c991574ac499e90937a5c036f5",
                null,
                "http://www.google.com",
                getDate(1).toString(),
                true,
            ),
        ];
    }

    addLink(isEditable: boolean): Promise<ReviewLink> {
        return new Promise<ReviewLink>((resolve) => {
            const reviewLink = new ReviewLink(
                uuidv4(),
                null,
                "http://www.google.com",
                getDate(5).toString(),
                isEditable,
            );
            this.links.push(reviewLink);
            resolve(reviewLink);
        });
    }

    delete(item: ReviewLink): void {
        const itemIndex = this.links.indexOf(item);
        if (itemIndex === -1) {
            return;
        }
        this.links.splice(itemIndex, 1);
    }

    share(): Promise<void> {
        return Promise.resolve();
    }

    edit(item: ReviewLink, validTo: Date, pinCode: string, displayName: string, visitorGroups: string[]): void {
        if (validTo) {
            item.validTo = validTo;
        }
        if (pinCode) {
            item.pinCode = pinCode;
        }
        item.displayName = displayName;
        item.visitorGroups = visitorGroups;
    }
}
