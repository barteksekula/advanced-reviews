import "./external-review-share-dialog.scss";

import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import TextField from "@mui/material/TextField";
import React, { useEffect, useState } from "react";

export interface LinkShareResult {
    email: string;
    subject: string;
    message: string;
}

interface ShareDialogProps {
    open: boolean;
    onClose(linkShare: LinkShareResult): void;
    initialSubject?: string;
    initialMessage?: string;
    resources: ExternalReviewResources;
}

const ShareDialog = ({ open, onClose, initialSubject, initialMessage, resources }: ShareDialogProps) => {
    const [email, setEmail] = useState<string>("");
    const [subject, setSubject] = useState<string>(initialSubject);
    const [isValidEmail, setIsValidEmail] = useState<boolean>(false);
    const [message, setMessage] = useState<string>(initialMessage);

    useEffect(() => {
        setEmail("");
        setMessage(initialMessage);
    }, [open]);

    const onEmailTextChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newValue = e.target.value;
        setEmail(newValue);
        setIsValidEmail(checkIsValidEmail(newValue));
    };

    const emailReg = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/;
    const checkIsValidEmail = (str: string): boolean => {
        return emailReg.test(str);
    };

    return (
        <Dialog open={open} onClose={() => onClose(null)}>
            <DialogTitle>{resources.sharedialog.dialogtitle}</DialogTitle>
            <DialogContent className="share-dialog-content">
                <TextField
                    label={resources.sharedialog.emailaddresslabel}
                    fullWidth
                    autoFocus
                    required
                    value={email}
                    onChange={onEmailTextChanged}
                    error={email !== "" && !isValidEmail}
                    margin="normal"
                />
                <TextField
                    label={resources.sharedialog.emailsubjectlabel}
                    fullWidth
                    required
                    value={subject}
                    onChange={(event: React.ChangeEvent<HTMLInputElement>) => setSubject(event.target.value)}
                    margin="normal"
                />
                <TextField
                    label={resources.sharedialog.emailmessagelabel}
                    fullWidth
                    required
                    multiline
                    rows={15}
                    value={message}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setMessage(e.target.value)}
                    margin="normal"
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={() => onClose(null)}>{resources.sharedialog.cancelbutton}</Button>
                <Button
                    variant="contained"
                    onClick={() => onClose({ email, subject, message })}
                    disabled={!isValidEmail}
                >
                    {resources.sharedialog.sendbutton}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ShareDialog;
