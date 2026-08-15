import type { InputHTMLAttributes, TextareaHTMLAttributes } from "react";

type BaseProps = {
  label: string;
  hint?: string;
};

type InputProps = BaseProps & {
  as?: "input";
  inputProps?: InputHTMLAttributes<HTMLInputElement>;
};

type TextareaProps = BaseProps & {
  as: "textarea";
  textareaProps?: TextareaHTMLAttributes<HTMLTextAreaElement>;
};

type Props = InputProps | TextareaProps;

export function FormField(props: Props) {
  return (
    <label className="field">
      <span className="field-label">{props.label}</span>
      {props.as === "textarea" ? (
        <textarea className="field-control field-area" {...props.textareaProps} />
      ) : (
        <input className="field-control" {...props.inputProps} />
      )}
      {props.hint ? <span className="field-hint">{props.hint}</span> : null}
    </label>
  );
}
