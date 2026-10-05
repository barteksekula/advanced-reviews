import "./external-review-manage-links.scss";

import Icon from "@mui/material/Icon";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Snackbar from "@mui/material/Snackbar";
import classNames from "classnames";
import { format } from "date-fns";
import { observer } from "mobx-react-lite";
import React, { useState } from "react";

import Confirmation from "../confirmation/confirmation";
import { IExternalReviewStore, ReviewLink } from "./external-review-links-store";
import LinkEditDialog from "./external-review-manage-links-edit";
import ShareDialog, { LinkShareResult } from "./external-review-share-dialog";

export interface VisitorGroup {
    id: string;
    name: string;
}

export interface ExternalReviewWidgetContentProps {
    store: IExternalReviewStore;
    resources: ExternalReviewResources;
    availableVisitorGroups: VisitorGroup[];
    editableLinksEnabled: boolean;
    allowAnonymousEditableLinks?: boolean;
    pinCodeSecurityEnabled: boolean;
    pinCodeSecurityRequired?: boolean;
    pinCodeLength: number;
    prolongDays: number;
}

/**
 * Component used to render list of external review links
 */
const ExternalReviewWidgetContent = observer(
    ({
        store,
        resources,
        availableVisitorGroups,
        editableLinksEnabled,
        allowAnonymousEditableLinks,
        pinCodeSecurityEnabled,
        pinCodeSecurityRequired,
        pinCodeLength,
        prolongDays,
    }: ExternalReviewWidgetContentProps) => {
        const [currentLinkToDelete, setLinkToDelete] = useState<ReviewLink>(null);
        const [currentLinkToShare, setLinkToShare] = useState<ReviewLink>(null);
        const [currentLinkToEdit, setLinkToEdit] = useState<ReviewLink>(null);
        const [shareResultMessage, setShareResultMessage] = useState<string>(null);
        const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

        const usesPinCode = (isEditable: boolean) =>
            pinCodeSecurityEnabled && (!isEditable || !!allowAnonymousEditableLinks);
        const isPinRequired = (isEditable: boolean) => usesPinCode(isEditable) && pinCodeSecurityRequired;

        const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
            setAnchorEl(event.currentTarget);
        };

        const handleMenuClose = () => {
            setAnchorEl(null);
        };

        const onDelete = (action: boolean) => {
            setLinkToDelete(null);
            if (!action) {
                return;
            }
            store.delete(currentLinkToDelete);
        };

        const onShareDialogClose = (shareLink: LinkShareResult) => {
            setLinkToShare(null);
            if (shareLink === null) {
                return;
            }
            store.share(currentLinkToShare, shareLink.email, shareLink.subject, shareLink.message).then(
                () => setShareResultMessage(resources.sharedialog.sendsucceeded),
                () => setShareResultMessage(resources.sharedialog.sendfailed),
            );
        };

        const onEditClose = async (validTo: Date, pinCode: string, displayName: string, visitorGroups: string[]) => {
            setLinkToEdit(null);
            if (!validTo) {
                return;
            }
            if (currentLinkToEdit.isPersisted) {
                store.edit(currentLinkToEdit, validTo, pinCode, displayName, visitorGroups);
                return;
            }

            if (isPinRequired(currentLinkToEdit.isEditable) && !pinCode) {
                return;
            }

            const reviewLink = await store.addLink(currentLinkToEdit.isEditable);
            store.edit(reviewLink, null, pinCode, displayName, visitorGroups);
        };

        const addNewLink = (isEditable) => {
            handleMenuClose();
            if (!isPinRequired(isEditable)) {
                store.addLink(isEditable);
                return;
            }

            const temporaryLink = new ReviewLink(null, null, null, null, isEditable);
            setLinkToEdit(temporaryLink);
        };

        if (!store.enabled) {
            return (
                <div className="empty-list">
                    <span>{resources.list.onlypages}</span>
                </div>
            );
        }

        return (
            <div className="external-review-links">
                {store.links.length === 0 && (
                    <div className="empty-list">
                        <span>{resources.list.emptylist}</span>
                    </div>
                )}

                {store.links.length > 0 && (
                    <List className="external-reviews-list" disablePadding>
                        {store.links.map((item: ReviewLink) => {
                            const typeName = item.isEditable
                                ? resources.list.editablelinkname
                                : resources.list.viewlinkname;
                            const name = item.displayName || typeName;

                            return (
                                <ListItem
                                    key={item.token}
                                    className={classNames("list-item", { inactive: !item.isActive })}
                                    disablePadding
                                >
                                    {editableLinksEnabled && (
                                        <Icon className="link-type" title={typeName}>
                                            {item.isEditable ? "rate_review" : "pageview"}
                                        </Icon>
                                    )}
                                    <div className="link-details">
                                        <div className="link-name">
                                            {item.isActive ? (
                                                <a
                                                    href={item.linkUrl}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    title={name}
                                                >
                                                    {name}
                                                </a>
                                            ) : (
                                                <span title={name}>{name}</span>
                                            )}
                                            {!item.displayName && (
                                                <span className="link-token" title={item.token}>
                                                    {item.token.substring(0, 8)}
                                                </span>
                                            )}
                                        </div>
                                        <div className="link-meta">
                                            <span>
                                                {resources.list.itemvalidto}:{" "}
                                                {format(item.validTo, "MMM d, yyyy HH:mm")}
                                            </span>
                                            {item.pinCode && pinCodeSecurityEnabled && (
                                                <Icon title={resources.list.editdialog.linksecured}>lock</Icon>
                                            )}
                                            {item.visitorGroups && item.visitorGroups.length > 0 && (
                                                <Icon title={resources.list.editdialog.visitorgroups}>groups</Icon>
                                            )}
                                            {item.projectId > 0 && (
                                                <span
                                                    className="dijitReset dijitInline dijitIcon epi-iconProject"
                                                    title={resources.list.projectname + ": " + item.projectName}
                                                ></span>
                                            )}
                                        </div>
                                    </div>
                                    <div className="link-actions">
                                        <IconButton
                                            size="small"
                                            title={resources.list.editlink}
                                            onClick={() => setLinkToEdit(item)}
                                        >
                                            <Icon fontSize="small">edit</Icon>
                                        </IconButton>
                                        <IconButton
                                            size="small"
                                            disabled={!item.isActive}
                                            title={resources.list.sharetitle}
                                            onClick={() => setLinkToShare(item)}
                                        >
                                            <Icon fontSize="small">share</Icon>
                                        </IconButton>
                                        <IconButton
                                            size="small"
                                            title={resources.list.deletetitle}
                                            onClick={() => setLinkToDelete(item)}
                                        >
                                            <Icon fontSize="small">delete_outline</Icon>
                                        </IconButton>
                                    </div>
                                </ListItem>
                            );
                        })}
                    </List>
                )}
                <div className="add-link">
                    {editableLinksEnabled ? (
                        <>
                            <IconButton title="Add link" onClick={handleMenuOpen}>
                                <Icon>playlist_add</Icon>
                            </IconButton>
                            <Menu
                                anchorEl={anchorEl}
                                open={Boolean(anchorEl)}
                                onClose={handleMenuClose}
                                anchorOrigin={{
                                    vertical: "bottom",
                                    horizontal: "left",
                                }}
                            >
                                <MenuItem onClick={() => addNewLink(false)}>
                                    <ListItemIcon>
                                        <Icon>pageview</Icon>
                                    </ListItemIcon>
                                    <ListItemText>{resources.list.viewlink}</ListItemText>
                                </MenuItem>
                                <MenuItem onClick={() => addNewLink(true)}>
                                    <ListItemIcon>
                                        <Icon>rate_review</Icon>
                                    </ListItemIcon>
                                    <ListItemText>{resources.list.editlink}</ListItemText>
                                </MenuItem>
                            </Menu>
                        </>
                    ) : (
                        <IconButton title="Add link" onClick={() => addNewLink(false)}>
                            <Icon>playlist_add</Icon>
                        </IconButton>
                    )}
                </div>
                {!!currentLinkToDelete && (
                    <Confirmation
                        title={resources.removedialog.title}
                        description={resources.removedialog.description}
                        okName={resources.removedialog.ok}
                        cancelName={resources.removedialog.cancel}
                        open={!!currentLinkToDelete}
                        onCloseDialog={onDelete}
                    />
                )}

                {!!currentLinkToShare && (
                    <ShareDialog
                        open={!!currentLinkToShare}
                        onClose={onShareDialogClose}
                        initialSubject={store.initialMailSubject}
                        initialMessage={
                            currentLinkToShare && currentLinkToShare.isEditable
                                ? store.initialEditMailMessage
                                : store.initialViewMailMessage
                        }
                        resources={resources}
                    />
                )}
                <Snackbar
                    open={!!shareResultMessage}
                    autoHideDuration={6000}
                    onClose={() => setShareResultMessage(null)}
                    message={shareResultMessage}
                />
                {!!currentLinkToEdit && (
                    <LinkEditDialog
                        reviewLink={currentLinkToEdit}
                        onClose={onEditClose}
                        resources={resources}
                        availableVisitorGroups={availableVisitorGroups}
                        open={!!currentLinkToEdit}
                        pinCodeSecurityEnabled={usesPinCode(currentLinkToEdit.isEditable)}
                        pinCodeSecurityRequired={pinCodeSecurityRequired}
                        pinCodeLength={pinCodeLength}
                        prolongDays={prolongDays}
                    />
                )}
            </div>
        );
    },
);

export default ExternalReviewWidgetContent;
