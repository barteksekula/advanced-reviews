import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import TextField from "@mui/material/TextField";
import React, { useState } from "react";

interface ConfirmDialogProps {
    open: boolean;
    onClose(userName: string): void;
    initialUserName?: string;
}

const ConfirmDialog = ({ open, onClose, initialUserName }: ConfirmDialogProps) => {
    const [userName, setUserName] = useState<string>(initialUserName || "");
    const trimmedUserName = userName.trim();

    const onDialogClose = () => {
        onClose(null);
    };

    const onSave = (event?: React.FormEvent) => {
        event?.preventDefault();
        if (!trimmedUserName) {
            return;
        }
        onClose(trimmedUserName);
    };

    return (
        <Dialog open={open} onClose={onDialogClose}>
            <form onSubmit={onSave}>
                <DialogTitle>Enter your name</DialogTitle>
                <DialogContent>
                    <p>Your name will be shown as the author of the comments you add.</p>
                    <TextField
                        label="Name"
                        autoFocus
                        required
                        fullWidth
                        value={userName}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => setUserName(e.target.value)}
                        error={!trimmedUserName}
                        margin="normal"
                    />
                </DialogContent>
                <DialogActions>
                    <Button type="submit" variant="contained" disabled={!trimmedUserName}>
                        Continue
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    );
};

export default ConfirmDialog;
