import "@material/react-dialog/index.scss";

import { Checkbox, Input, TextButton, TextField } from "@episerver/ui-framework";
import Dialog, { DialogButton, DialogContent, DialogFooter, DialogTitle } from "@material/react-dialog";
import { format, parse } from "date-fns";
import { observer } from "mobx-react-lite";
import React, { useState } from "react";

import { ReviewLink } from "./external-review-links-store";
import { VisitorGroup } from "./external-review-manage-links";

interface LinkEditDialogProps {
    reviewLink: ReviewLink;
    onClose(validTo: Date, pinCode: string, displayName: string, visitorGroups: string[]): void;
    resources: ExternalReviewResources;
    availableVisitorGroups: VisitorGroup[];
    open: boolean;
    pinCodeSecurityEnabled: boolean;
    pinCodeSecurityRequired: boolean;
    pinCodeLength: number;
    prolongDays: number;
}

const dateTimeFormat = "yyyy-MM-dd hh:mm";

/**
 * Dialog component used to edit link created in manage links
 */
const LinkEditDialog = observer(
    ({
        reviewLink,
        onClose,
        open,
        resources,
        availableVisitorGroups,
        pinCodeSecurityEnabled,
        pinCodeSecurityRequired,
        pinCodeLength,
        prolongDays,
    }: LinkEditDialogProps) => {
        const [displayName, setDisplayName] = useState<string>(reviewLink.displayName || "");
        const [visitorGroups, setVisitorGroups] = useState<string[]>(reviewLink.visitorGroups || []);
        const [validDate, setValidDate] = useState<string>(format(reviewLink.validTo, dateTimeFormat));
        const [prolongVisible, setProlongVisible] = useState<boolean>(true);
        const [pinCode, setPinCode] = useState<string>(reviewLink.pinCode || "");
        const [shouldUpdatePinCode, setShouldUpdatePinCode] = useState<boolean>(!reviewLink.pinCode);
        const [canSave, setCanSave] = useState(!pinCodeSecurityRequired || !!reviewLink.pinCode);

        if (prolongDays <= 0) {
            prolongDays = 5;
        }

        const onCloseDialog = (action: string) => {
            if (validDate && action !== "save") {
                onClose(null, null, null, null);
                return;
            }
            const newPin = shouldUpdatePinCode ? pinCode : null;
            const validTo = parse(validDate, dateTimeFormat, validDate);
            onClose(validTo, newPin, displayName, visitorGroups);
        };

        const updatePinCode = (event: React.FormEvent<HTMLInputElement>) => {
            const newValue = event.currentTarget.value;
            if (!!newValue && !/^\d+$/.test(newValue)) {
                return;
            }
            setPinCode(newValue);
            const doesPinMatchRequirements = newValue.length === pinCodeLength;
            setCanSave(
                pinCodeSecurityRequired ? doesPinMatchRequirements : newValue.length === 0 || doesPinMatchRequirements,
            );
        };

        const updateValidDate = () => {
            setProlongVisible(false);
            let dateCopy = new Date(reviewLink.validTo.getTime());
            const today = new Date();
            if (today > dateCopy) {
                dateCopy = today;
            }
            dateCopy.setDate(dateCopy.getDate() + prolongDays);

            setValidDate(format(dateCopy, dateTimeFormat));
        };

        const prolongTitle = (resources.list.editdialog.prolongbydays || "").replace(
            "[#days#]",
            prolongDays.toString(),
        );

        return (
            <Dialog open={open} scrimClickAction="" escapeKeyAction="" onClose={onCloseDialog}>
                <DialogTitle>
                    {reviewLink.isPersisted ? resources.list.editdialog.title : resources.list.editdialog.newitemtitle}
                </DialogTitle>
                <DialogContent className="external-link-edit-dialog">
                    {reviewLink.isPersisted && (
                        <div className="field-group prolong">
                            <span>
                                {resources.list.editdialog.validto}: {validDate}
                            </span>{" "}
                            {prolongVisible && (
                                <TextButton title={prolongTitle} onClick={updateValidDate}>
                                    Prolong
                                </TextButton>
                            )}
                        </div>
                    )}
                    {pinCodeSecurityEnabled && !reviewLink.isEditable && (
                        <div className="field-group">
                            {!!reviewLink.pinCode && (
                                <>
                                    <Checkbox
                                        nativeControlId="pinCodeActive"
                                        checked={shouldUpdatePinCode}
                                        onChange={() => setShouldUpdatePinCode(!shouldUpdatePinCode)}
                                    />
                                    <label className="checkbox-label" htmlFor="pinCodeActive">
                                        {resources.list.editdialog.pincheckboxlabel}
                                    </label>
                                </>
                            )}
                            <TextField
                                label={`${resources.list.editdialog.pincode} (${pinCodeLength} ${resources.list.editdialog.digits})`}
                                style={{ width: "100%" }}
                            >
                                <Input
                                    name="pin-code"
                                    autoComplete="new-password"
                                    disabled={!shouldUpdatePinCode}
                                    value={pinCode}
                                    onChange={updatePinCode}
                                    required={pinCodeSecurityRequired}
                                    type="password"
                                    maxLength={pinCodeLength}
                                />
                            </TextField>
                            <div className="mdc-text-field-helper-line">
                                <p className="mdc-text-field-helper-text mdc-text-field-helper-text--persistent">
                                    {!!pinCode
                                        ? resources.list.editdialog.linksecured
                                        : resources.list.editdialog.linknotsecured}
                                </p>
                            </div>
                        </div>
                    )}
                    <div className="field-group">
                        <TextField label={resources.list.editdialog.displayname} style={{ width: "100%" }} autoFocus>
                            <Input
                                name="display-name"
                                value={displayName}
                                onChange={(event: React.FormEvent<HTMLInputElement>) =>
                                    setDisplayName(event.currentTarget.value)
                                }
                            />
                        </TextField>
                        <div className="mdc-text-field-helper-line">
                            <p className="mdc-text-field-helper-text mdc-text-field-helper-text--persistent">
                                {resources.list.editdialog.displaynamehelptext}
                            </p>
                        </div>
                    </div>
                    <div className="field-group">
                        <span>
                            {resources.list.editdialog.visitorgroups} ({visitorGroups.length})
                        </span>
                        <div className="visitor-groups-list">
                            {availableVisitorGroups.map((v) => (
                                <div key={v.id}>
                                    <Checkbox
                                        nativeControlId={v.id}
                                        checked={visitorGroups.indexOf(v.id) !== -1}
                                        onChange={(e) => {
                                            const selectedGroups = [...visitorGroups];
                                            if (e.target.checked) {
                                                setVisitorGroups([...selectedGroups, v.id]);
                                            } else {
                                                selectedGroups.splice(selectedGroups.indexOf(v.id), 1);
                                                setVisitorGroups([...selectedGroups]);
                                            }
                                        }}
                                    />
                                    <label className="checkbox-label" htmlFor={v.id}>
                                        {v.name}
                                    </label>
                                </div>
                            ))}
                        </div>
                        <div className="mdc-text-field-helper-line">
                            <p className="mdc-text-field-helper-text mdc-text-field-helper-text--persistent">
                                {resources.list.editdialog.visitorgroupshelptext}
                            </p>
                        </div>
                    </div>
                </DialogContent>
                <DialogFooter>
                    <DialogButton dense action="cancel">
                        {resources.shared.cancel}
                    </DialogButton>
                    <DialogButton disabled={!canSave} raised dense action="save" isDefault>
                        {resources.shared.save}
                    </DialogButton>
                </DialogFooter>
            </Dialog>
        );
    },
);

export default LinkEditDialog;
