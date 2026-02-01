import { format, formatDistance } from "date-fns";
import { da } from "date-fns/locale/da";
import { de } from "date-fns/locale/de";
import { enUS } from "date-fns/locale/en-US";
import { es } from "date-fns/locale/es";
import { fi } from "date-fns/locale/fi";
import { fr } from "date-fns/locale/fr";
import { it } from "date-fns/locale/it";
import { ja } from "date-fns/locale/ja";
import { nb } from "date-fns/locale/nb"; // date-fns uses bokmål as the default norwegian culture, not nynorsk as in epi
import { nl } from "date-fns/locale/nl";
import { sv } from "date-fns/locale/sv";
import { zhCN } from "date-fns/locale/zh-CN";
import { action, computed, makeAutoObservable, makeObservable, observable } from "mobx";

const locales = {
    da,
    de,
    en: enUS,
    es,
    fi,
    fr,
    it,
    ja,
    nl,
    no: nb,
    sv,
    zh_cn: zhCN,
};

/**
 * Represents a comment added by user
 */
export class Comment {
    author: string;
    text: string;
    date: Date;
    screenshot: string;

    store: IReviewComponentStore;

    constructor(store: IReviewComponentStore) {
        this.store = store;

        makeObservable(this, {
            formattedDate: computed,
            userFriendlyDate: computed,
        });
    }

    get formattedDate() {
        if (!this.date) {
            return "";
        }
        return format(this.date, "MMM do yyyy");
    }

    get userFriendlyDate() {
        if (!this.date) {
            return "";
        }

        const options: any = { addSuffix: true };
        if (this.store && this.store.currentLocale) {
            options.locale = locales[this.store.currentLocale.toLowerCase()];
        }

        return formatDistance(this.date, new Date(), options);
    }

    static create(
        author: string,
        text: string,
        store: IReviewComponentStore,
        date?: Date,
        screenshot?: string,
    ): Comment {
        const instance = new Comment(store);
        instance.author = author;
        instance.text = text;
        instance.date = date || new Date();
        instance.screenshot = screenshot;
        return instance;
    }
}

interface UsersLastReadHashmap {
    [userName: string]: Date;
}

export interface NewPinDto {
    currentCommentText: string;
    currentPriority: Priority;
    currentScreenshot: string;
    screenshotMode: boolean;
}

export interface Dimensions {
    x: number;
    y: number;
}

export interface PinPositioningDetails {
    documentRelativePosition: Dimensions;
    documentSize: Dimensions;
    propertyName?: string;
    propertyPosition?: Dimensions;
    propertySize?: Dimensions;
    clickedDomNodeSelector?: string;
    clickedDomNodeSize?: Dimensions;
    clickedDomNodePosition?: Dimensions;
}

export class PinLocation implements PinPositioningDetails {
    id: string;
    propertyName?: string;
    isDone: boolean = false;
    documentRelativePosition: Dimensions;
    documentSize: Dimensions;
    propertyPosition?: Dimensions;
    propertySize?: Dimensions;
    blockId?: string;
    blockName?: string;
    blockPosition?: Dimensions;
    blockSize?: Dimensions;
    clickedDomNodeSelector?: string;
    clickedDomNodeSize?: Dimensions;
    clickedDomNodePosition?: Dimensions;
    priority: Priority = Priority.Normal;
    comments: Comment[] = [];
    /**
     * FirstComment is a main comment added when saving review location for the first time
     */
    firstComment: Comment = null;

    get formattedFirstComment(): string {
        if (!this.firstComment.date) {
            return "";
        }
        const comment = this.firstComment;

        let result = "";
        if (comment.author && comment.author.trim()) {
            result += comment.author + ":";
        }

        if (comment.text && comment.text.trim()) {
            result += comment.text + ",";
        }

        result += comment.userFriendlyDate;

        return result;
    }

    get displayName(): string {
        return this.propertyName || this.firstComment.text;
    }

    /**
     * List of users and date when they last saw the review.
     */
    usersLastRead: UsersLastReadHashmap = {};

    private _rootStore: IReviewComponentStore;

    constructor(rootStore: IReviewComponentStore, point: any) {
        makeObservable(this, {
            isDone: observable,
            priority: observable,
            comments: observable,
            firstComment: observable,
            usersLastRead: observable,
            formattedFirstComment: computed,
            displayName: computed,
            isUpdatedReview: computed,
            updateCurrentUserLastRead: action,
            clearLastUsersRead: action,
        });

        this._rootStore = rootStore;
        this.firstComment = new Comment(this._rootStore);
        Object.keys(point).forEach((key) => (this[key] = point[key]));
    }

    updateCurrentUserLastRead(): void {
        if (!this._rootStore) {
            return;
        }
        this.usersLastRead[this._rootStore.currentUser] = new Date();
    }

    clearLastUsersRead(): void {
        this.usersLastRead = {};
    }

    private ensureDateTimeObject = (obj) => {
        return typeof obj === "string" ? new Date(obj) : obj;
    };

    get isUpdatedReview() {
        if (!this._rootStore) {
            return false;
        }

        const currentUser = this._rootStore.currentUser;

        const allComments = this.comments.slice();
        if (this.firstComment.date) {
            allComments.push(this.firstComment);
        }
        if (allComments.length === 0) {
            return false;
        }

        const otherUserComments = allComments
            .filter((x) => x.author !== currentUser)
            .map((x) => x.date)
            .sort();
        const lastOtherUserComment = otherUserComments.length > 0 ? otherUserComments.pop() : null;
        if (!lastOtherUserComment) {
            // there are no comments added by other users
            return false;
        }

        const currentUserComments = allComments
            .filter((x) => x.author === currentUser)
            .map((x) => x.date)
            .sort();
        const lastCurrentUserComment = currentUserComments.length > 0 ? currentUserComments.pop() : null;
        if (
            lastCurrentUserComment !== null &&
            this.ensureDateTimeObject(lastCurrentUserComment) > this.ensureDateTimeObject(lastOtherUserComment)
        ) {
            // current user has the last comment
            return false;
        }

        const lastCurrentUserReadDate = this.usersLastRead[this._rootStore.currentUser];
        if (!lastCurrentUserReadDate) {
            // current user hsa no comments
            return true;
        }
        if (this.ensureDateTimeObject(lastCurrentUserReadDate) > this.ensureDateTimeObject(lastOtherUserComment)) {
            return false;
        }

        return true;
    }
}

export interface ExternalReviewOptions {
    allowScreenshotAttachments: boolean;
}

export enum Priority {
    Important = "Important",
    Normal = "Normal",
    Trivial = "Trivial",
}

export interface IReviewComponentStore {
    reviewLocations: PinLocation[];

    /**
     * Currently logged user.
     * Field is used when saving comment author
     */
    currentUser: string;

    currentLocale: string;

    avatarUrl: string;

    /**
     * dictionary with mappings property name to property displayname
     */
    propertyNameMapping: object;

    options: ExternalReviewOptions;

    editedPinLocation: PinLocation;

    selectedPinLocation: PinLocation;

    selectedPinLocationIndex: number;

    toggleResolve(): Promise<PinLocation>;

    addComment(commentText: string, screenshot?: string): Promise<PinLocation>;

    remove(pin: PinLocation): Promise<void>;

    save(state: NewPinDto, reviewLocation: PinLocation): Promise<PinLocation>;

    load(): void;

    enable(): void;
    disable(): void;

    /**
     * @param propertyNameMapping update mapping dictionary
     */
    updateDisplayNamesDictionary(propertyNameMapping: object): void;

    /**
     * return property display name
     * @param propertyName property name
     */
    resolvePropertyDisplayName(propertyName: string): string;

    getUserAvatarUrl(userName: string): string;

    filteredReviewLocations: PinLocation[];

    filter: ReviewCollectionFilter;
}

class ReviewCollectionFilter {
    reviewMode: boolean = true;
    showUnread: boolean = true;
    showActive: boolean = true;
    showResolved: boolean = false;

    constructor() {
        makeAutoObservable(this);
    }
}

class ReviewComponentStore implements IReviewComponentStore {
    reviewLocations: PinLocation[] = [];
    editedPinLocation: PinLocation = null;
    selectedPinLocation: PinLocation = null;
    propertyNameMapping: object = null;
    enabled: boolean = false;
    options: ExternalReviewOptions = null;

    filter: ReviewCollectionFilter;

    currentUser = "";

    currentLocale = "en";

    avatarUrl = "";

    _advancedReviewService: any;

    constructor(advancedReviewService: AdvancedReviewService) {
        makeObservable(this, {
            reviewLocations: observable,
            editedPinLocation: observable,
            selectedPinLocation: observable,
            propertyNameMapping: observable,
            options: observable,
            selectedPinLocationIndex: computed,
            filteredReviewLocations: computed,
            load: action.bound,
            enabled: observable,
            updateDisplayNamesDictionary: action.bound,
            resolvePropertyDisplayName: action,
            toggleResolve: action.bound,
            addComment: action.bound,
            save: action.bound,
            remove: action.bound,
            getUserAvatarUrl: action.bound,
        });

        this._advancedReviewService = advancedReviewService;
        this.filter = new ReviewCollectionFilter();
    }

    private parseComment(json: any): Comment {
        return json
            ? Comment.create(json.author, json.text, this, json.date, json.screenshot)
            : Comment.create("", "", this);
    }

    load(): void {
        this._advancedReviewService.load().then((reviewLocations) => {
            this.reviewLocations = reviewLocations.map((x: any) => {
                return new PinLocation(this, {
                    id: x.id,
                    documentRelativePosition: x.data.documentRelativePosition,
                    documentSize: x.data.documentSize,
                    propertyPosition: x.data.propertyPosition,
                    propertySize: x.data.propertySize,
                    propertyName: x.data.propertyName,
                    clickedDomNodeSelector: x.data.clickedDomNodeSelector,
                    clickedDomNodeSize: x.data.clickedDomNodeSize,
                    clickedDomNodePosition: x.data.clickedDomNodePosition,
                    priority: x.data.priority,
                    isDone: x.data.isDone,
                    firstComment: this.parseComment(x.data.firstComment),
                    comments: (x.data.comments || []).map((x: any) => this.parseComment(x)),
                });
            });
        });
    }

    enable() {
        this.enabled = true;
    }

    disable() {
        this.enabled = false;
    }

    get selectedPinLocationIndex(): number {
        if (!this.selectedPinLocation) {
            return -1;
        }

        return this.reviewLocations.indexOf(this.selectedPinLocation);
    }

    get filteredReviewLocations(): PinLocation[] {
        return this.reviewLocations.filter((location) => {
            return (
                (this.filter.showResolved && location.isDone) ||
                (this.filter.showActive && !location.isDone && !location.isUpdatedReview) ||
                (this.filter.showUnread && location.isUpdatedReview)
            );
        });
    }

    updateDisplayNamesDictionary(propertyNameMapping: object): void {
        this.propertyNameMapping = propertyNameMapping;
    }

    resolvePropertyDisplayName(propertyName: string): string {
        if (!this.propertyNameMapping) {
            return propertyName;
        }

        if (!propertyName) {
            return propertyName;
        }

        const displayName = this.propertyNameMapping[propertyName];
        return displayName;
    }

    toggleResolve(): Promise<PinLocation> {
        this.editedPinLocation.isDone = !this.editedPinLocation.isDone;
        return this.saveLocation(this.editedPinLocation);
    }

    addComment(commentText: string, screenshot?: string): Promise<PinLocation> {
        const comment = Comment.create(this.currentUser, commentText, this, null, screenshot);
        this.editedPinLocation.comments.push(comment);
        this.editedPinLocation.clearLastUsersRead();
        return this.saveLocation(this.editedPinLocation);
    }

    //TODO: convert to async method
    save(item: NewPinDto, editedReview: PinLocation): Promise<PinLocation> {
        editedReview.priority = item.currentPriority;
        editedReview.firstComment = Comment.create(
            this.currentUser,
            item.currentCommentText,
            this,
            null,
            item.currentScreenshot,
        );
        return this.saveLocation(editedReview);
    }

    remove(pin: PinLocation): Promise<void> {
        return new Promise((resolve, reject) => {
            this._advancedReviewService
                .remove(pin.id)
                .then(() => {
                    this.reviewLocations.splice(this.reviewLocations.indexOf(pin), 1);
                    resolve();
                })
                .otherwise(() => {
                    reject();
                });
        });
    }

    getUserAvatarUrl(userName: string): string {
        const encodedUserName = encodeURIComponent(userName);
        return `${this.avatarUrl}/${encodedUserName}`;
    }

    private saveLocation(reviewLocation: PinLocation): Promise<PinLocation> {
        return new Promise((resolve, reject) => {
            const data = {
                propertyName: reviewLocation.propertyName,
                isDone: reviewLocation.isDone,
                documentRelativePosition: reviewLocation.documentRelativePosition,
                documentSize: reviewLocation.documentSize,
                propertyPosition: reviewLocation.propertyPosition,
                propertySize: reviewLocation.propertySize,
                clickedDomNodeSelector: reviewLocation.clickedDomNodeSelector,
                clickedDomNodePosition: reviewLocation.clickedDomNodePosition,
                clickedDomNodeSize: reviewLocation.clickedDomNodeSize,
                priority: reviewLocation.priority,
                comments: reviewLocation.comments.map((x: any) => {
                    return {
                        author: x.author,
                        date: x.date,
                        screenshot: x.screenshot,
                        text: x.text,
                    };
                }),
                firstComment: {
                    author: reviewLocation.firstComment.author,
                    date: reviewLocation.firstComment.date,
                    screenshot: reviewLocation.firstComment.screenshot,
                    text: reviewLocation.firstComment.text,
                },
            };
            this._advancedReviewService
                .add(reviewLocation.id, data)
                .then((result) => {
                    if (reviewLocation.id !== result.id) {
                        reviewLocation.id = result.id;
                        this.reviewLocations.push(reviewLocation);
                    }
                    resolve(reviewLocation);
                })
                .otherwise((e) => {
                    reject(e);
                });
        });
    }
}

export const createStores = (advancedReviewService: AdvancedReviewService, res: ReviewResources): any => {
    return {
        reviewStore: new ReviewComponentStore(advancedReviewService),
        resources: res,
    };
};
