/*

This file is part of the iText (R) project.
Copyright (c) 1998-2019 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using iText.Forms.Util;
using iText.IO.Codec;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.IO.Source;
using iText.IO.Util;
using iText.Kernel;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace iText.Forms.Fields {
    /// <summary>
    /// This class represents a single field or field group in an
    /// <see cref="iText.Forms.PdfAcroForm">AcroForm</see>
    /// .
    /// <br /><br />
    /// To be able to be wrapped with this
    /// <see cref="iText.Kernel.Pdf.PdfObjectWrapper{T}"/>
    /// the
    /// <see cref="iText.Kernel.Pdf.PdfObject"/>
    /// must be indirect.
    /// </summary>
    public class PdfFormField : PdfObjectWrapper<PdfDictionary> {
        /// <summary>
        /// Flag that designates, if set, that the field can contain multiple lines
        /// of text.
        /// </summary>
        public static readonly int FF_MULTILINE = MakeFieldFlag(13);

        /// <summary>Flag that designates, if set, that the field's contents must be obfuscated.</summary>
        public static readonly int FF_PASSWORD = MakeFieldFlag(14);

        /// <summary>Size of text in form fields when font size is not explicitly set.</summary>
        [System.ObsoleteAttribute(@"Will be made package-private in iText 7.2.")]
        public const int DEFAULT_FONT_SIZE = 12;

        [System.ObsoleteAttribute(@"Will be made package-private in iText 7.2.")]
        public const int MIN_FONT_SIZE = 4;

        [System.ObsoleteAttribute(@"Will be made package-private in iText 7.2.")]
        public const int DA_FONT = 0;

        [System.ObsoleteAttribute(@"Will be made package-private in iText 7.2.")]
        public const int DA_SIZE = 1;

        [System.ObsoleteAttribute(@"Will be made package-private in iText 7.2.")]
        public const int DA_COLOR = 2;

        public const int ALIGN_LEFT = 0;

        public const int ALIGN_CENTER = 1;

        public const int ALIGN_RIGHT = 2;

        /// <summary>A field with the symbol check</summary>
        public const int TYPE_CHECK = 1;

        /// <summary>A field with the symbol circle</summary>
        public const int TYPE_CIRCLE = 2;

        /// <summary>A field with the symbol cross</summary>
        public const int TYPE_CROSS = 3;

        /// <summary>A field with the symbol diamond</summary>
        public const int TYPE_DIAMOND = 4;

        /// <summary>A field with the symbol square</summary>
        public const int TYPE_SQUARE = 5;

        /// <summary>A field with the symbol star</summary>
        public const int TYPE_STAR = 6;

        public const int HIDDEN = 1;

        public const int VISIBLE_BUT_DOES_NOT_PRINT = 2;

        public const int HIDDEN_BUT_PRINTABLE = 3;

        public const int VISIBLE = 4;

        public static readonly int FF_READ_ONLY = MakeFieldFlag(1);

        public static readonly int FF_REQUIRED = MakeFieldFlag(2);

        public static readonly int FF_NO_EXPORT = MakeFieldFlag(3);

        [System.ObsoleteAttribute(@"Will be made package-private in iText 7.2.")]
        public const float X_OFFSET = 2;

        protected internal static String[] typeChars = new String[] { "4", "l", "8", "u", "n", "H" };

        protected internal String text;

        protected internal ImageData img;

        protected internal PdfFont font;

        protected internal float fontSize = -1;

        protected internal Color color;

        protected internal int checkType;

        protected internal float borderWidth = 1;

        protected internal Color backgroundColor;

        protected internal Color borderColor;

        protected internal int rotation = 0;

        protected internal PdfFormXObject form;

        protected internal PdfAConformanceLevel pdfAConformanceLevel;

        /// <summary>
        /// Creates a form field as a wrapper object around a
        /// <see cref="iText.Kernel.Pdf.PdfDictionary"/>
        /// .
        /// This
        /// <see cref="iText.Kernel.Pdf.PdfDictionary"/>
        /// must be an indirect object.
        /// </summary>
        /// <param name="pdfObject">the dictionary to be wrapped, must have an indirect reference.</param>
        public PdfFormField(PdfDictionary pdfObject)
            : base(pdfObject) {
            EnsureObjectIsAddedToDocument(pdfObject);
            SetForbidRelease();
            RetrieveStyles();
        }

        /// <summary>
        /// Creates a minimal
        /// <see cref="PdfFormField"/>
        /// .
        /// </summary>
        /// <param name="pdfDocument">The document</param>
        protected internal PdfFormField(PdfDocument pdfDocument)
            : this((PdfDictionary)new PdfDictionary().MakeIndirect(pdfDocument)) {
            PdfName formType = GetFormType();
            if (formType != null) {
                Put(PdfName.FT, formType);
            }
        }

        /// <summary>
        /// Creates a form field as a parent of a
        /// <see cref="iText.Kernel.Pdf.Annot.PdfWidgetAnnotation"/>
        /// .
        /// </summary>
        /// <param name="widget">
        /// The widget which will be a kid of the
        /// <see cref="PdfFormField"/>
        /// </param>
        /// <param name="pdfDocument">The document</param>
        protected internal PdfFormField(PdfWidgetAnnotation widget, PdfDocument pdfDocument)
            : this((PdfDictionary)new PdfDictionary().MakeIndirect(pdfDocument)) {
            widget.MakeIndirect(pdfDocument);
            AddKid(widget);
            Put(PdfName.FT, GetFormType());
        }

        /// <summary>Makes a field flag by bit position.</summary>
        /// <remarks>
        /// Makes a field flag by bit position. Bit positions are numbered 1 to 32.
        /// But position 0 corresponds to flag 1, position 3 corresponds to flag 4 etc.
        /// </remarks>
        /// <param name="bitPosition">bit position of a flag in range 1 to 32 from the pdf specification.</param>
        /// <returns>corresponding field flag.</returns>
        public static int MakeFieldFlag(int bitPosition) {
            return (1 << (bitPosition - 1));
        }

        /// <summary>
        /// Creates an empty form field without a predefined set of layout or
        /// behavior.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the field in
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfFormField"/>
        /// </returns>
        public static iText.Forms.Fields.PdfFormField CreateEmptyField(PdfDocument doc) {
            return CreateEmptyField(doc, null);
        }

        /// <summary>
        /// Creates an empty form field without a predefined set of layout or
        /// behavior.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the field in
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfFormField"/>
        /// </returns>
        public static iText.Forms.Fields.PdfFormField CreateEmptyField(PdfDocument doc, PdfAConformanceLevel pdfAConformanceLevel
            ) {
            iText.Forms.Fields.PdfFormField field = new iText.Forms.Fields.PdfFormField(doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            return field;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfButtonFormField">button form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the button field in
        /// </param>
        /// <param name="rect">the location on the page for the button</param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreateButton(PdfDocument doc, Rectangle rect, int flags) {
            return CreateButton(doc, rect, flags, null);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfButtonFormField">button form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the button field in
        /// </param>
        /// <param name="rect">the location on the page for the button</param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreateButton(PdfDocument doc, Rectangle rect, int flags, PdfAConformanceLevel
             pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            PdfButtonFormField field = new PdfButtonFormField(annot, doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            if (null != pdfAConformanceLevel) {
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            field.SetFieldFlags(flags);
            return field;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfButtonFormField">button form field</see>
        /// with custom
        /// behavior and layout.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the button field in
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreateButton(PdfDocument doc, int flags) {
            return CreateButton(doc, flags, null);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfButtonFormField">button form field</see>
        /// with custom
        /// behavior and layout.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the button field in
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreateButton(PdfDocument doc, int flags, PdfAConformanceLevel pdfAConformanceLevel
            ) {
            PdfButtonFormField field = new PdfButtonFormField(doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            field.SetFieldFlags(flags);
            return field;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfTextFormField">text form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc) {
            return CreateText(doc, (PdfAConformanceLevel)null);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfTextFormField">text form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, PdfAConformanceLevel pdfAConformanceLevel) {
            PdfTextFormField textFormField = new PdfTextFormField(doc);
            textFormField.pdfAConformanceLevel = pdfAConformanceLevel;
            return textFormField;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfTextFormField">text form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, Rectangle rect) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            return new PdfTextFormField(annot, doc);
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">text form field</see>
        /// with an initial
        /// value, and the form's default font specified in
        /// <see cref="iText.Forms.PdfAcroForm.GetDefaultResources()"/>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, Rectangle rect, String name) {
            return CreateText(doc, rect, name, "");
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">text form field</see>
        /// with an initial
        /// value, and the form's default font specified in
        /// <see cref="iText.Forms.PdfAcroForm.GetDefaultResources()"/>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, Rectangle rect, String name, String value) {
            return CreateText(doc, rect, name, value, null, -1);
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">text form field</see>
        /// with an initial
        /// value, with a specified font and font size.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, Rectangle rect, String name, String value, PdfFont
             font, float fontSize) {
            return CreateText(doc, rect, name, value, font, fontSize, false);
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">text form field</see>
        /// with an initial
        /// value, with a specified font and font size.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <param name="multiline">true for multiline text field</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, Rectangle rect, String name, String value, PdfFont
             font, float fontSize, bool multiline) {
            return CreateText(doc, rect, name, value, font, fontSize, multiline, null);
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">text form field</see>
        /// with an initial
        /// value, with a specified font and font size.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <param name="multiline">true for multiline text field</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateText(PdfDocument doc, Rectangle rect, String name, String value, PdfFont
             font, float fontSize, bool multiline, PdfAConformanceLevel pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            PdfTextFormField field = new PdfTextFormField(annot, doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            if (null != pdfAConformanceLevel) {
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            field.SetMultiline(multiline);
            field.font = font;
            field.fontSize = fontSize;
            field.SetValue(value);
            field.SetFieldName(name);
            return field;
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">multilined text form field</see>
        /// with an initial
        /// value, with a specified font and font size.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateMultilineText(PdfDocument doc, Rectangle rect, String name, String value
            , PdfFont font, float fontSize) {
            return CreateText(doc, rect, name, value, font, fontSize, true);
        }

        /// <summary>
        /// Creates a named
        /// <see cref="PdfTextFormField">multiline text form field</see>
        /// with an initial
        /// value, and the form's default font specified in
        /// <see cref="iText.Forms.PdfAcroForm.GetDefaultResources()"/>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the text field in
        /// </param>
        /// <param name="rect">the location on the page for the text field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <returns>
        /// a new
        /// <see cref="PdfTextFormField"/>
        /// </returns>
        public static PdfTextFormField CreateMultilineText(PdfDocument doc, Rectangle rect, String name, String value
            ) {
            return CreateText(doc, rect, name, value, null, -1, true);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, int flags) {
            return CreateChoice(doc, flags, null);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, int flags, PdfAConformanceLevel pdfAConformanceLevel
            ) {
            PdfChoiceFormField field = new PdfChoiceFormField(doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            field.SetFieldFlags(flags);
            return field;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, Rectangle rect, int flags) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            PdfChoiceFormField field = new PdfChoiceFormField(annot, doc);
            field.SetFieldFlags(flags);
            return field;
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">
        /// an array of
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// objects that each represent
        /// the 'on' state of one of the choices.
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, Rectangle rect, String name, String value, 
            PdfArray options, int flags) {
            try {
                return CreateChoice(doc, rect, name, value, PdfFontFactory.CreateFont(), (float)DEFAULT_FONT_SIZE, options
                    , flags);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">
        /// an array of
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// objects that each represent
        /// the 'on' state of one of the choices.
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, Rectangle rect, String name, String value, 
            PdfArray options, int flags, PdfFont font, PdfAConformanceLevel pdfAConformanceLevel) {
            return CreateChoice(doc, rect, name, value, font, (float)DEFAULT_FONT_SIZE, options, flags, pdfAConformanceLevel
                );
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <param name="options">
        /// an array of
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// objects that each represent
        /// the 'on' state of one of the choices.
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, Rectangle rect, String name, String value, 
            PdfFont font, float fontSize, PdfArray options, int flags) {
            return CreateChoice(doc, rect, name, value, font, fontSize, options, flags, null);
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">choice form field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <param name="options">
        /// an array of
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// objects that each represent
        /// the 'on' state of one of the choices.
        /// </param>
        /// <param name="flags">
        /// an <code>int</code>, containing a set of binary behavioral
        /// flags. Do binary <code>OR</code> on this <code>int</code> to set the
        /// flags you require.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// </returns>
        public static PdfChoiceFormField CreateChoice(PdfDocument doc, Rectangle rect, String name, String value, 
            PdfFont font, float fontSize, PdfArray options, int flags, PdfAConformanceLevel pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            iText.Forms.Fields.PdfFormField field = new PdfChoiceFormField(annot, doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            if (null != pdfAConformanceLevel) {
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            field.font = font;
            field.fontSize = fontSize;
            field.Put(PdfName.Opt, options);
            field.SetFieldFlags(flags);
            field.SetFieldName(name);
            field.GetPdfObject().Put(PdfName.V, new PdfString(value, PdfEncodings.UNICODE_BIG));
            if ((flags & PdfChoiceFormField.FF_COMBO) == 0) {
                value = iText.Forms.Fields.PdfFormField.OptionsArrayToString(options);
            }
            PdfFormXObject xObject = new PdfFormXObject(new Rectangle(0, 0, rect.GetWidth(), rect.GetHeight()));
            field.DrawMultiLineTextAppearance(rect, font, fontSize, value, xObject);
            xObject.GetResources().AddFont(doc, font);
            annot.SetNormalAppearance(xObject.GetPdfObject());
            return (PdfChoiceFormField)field;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfSignatureFormField">signature form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the signature field in
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfSignatureFormField"/>
        /// </returns>
        public static PdfSignatureFormField CreateSignature(PdfDocument doc) {
            return CreateSignature(doc, (PdfAConformanceLevel)null);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfSignatureFormField">signature form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the signature field in
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfSignatureFormField"/>
        /// </returns>
        public static PdfSignatureFormField CreateSignature(PdfDocument doc, PdfAConformanceLevel pdfAConformanceLevel
            ) {
            PdfSignatureFormField signatureFormField = new PdfSignatureFormField(doc);
            signatureFormField.pdfAConformanceLevel = pdfAConformanceLevel;
            return signatureFormField;
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfSignatureFormField">signature form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the signature field in
        /// </param>
        /// <param name="rect">the location on the page for the signature field</param>
        /// <returns>
        /// a new
        /// <see cref="PdfSignatureFormField"/>
        /// </returns>
        public static PdfSignatureFormField CreateSignature(PdfDocument doc, Rectangle rect) {
            return CreateSignature(doc, rect, null);
        }

        /// <summary>
        /// Creates an empty
        /// <see cref="PdfSignatureFormField">signature form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the signature field in
        /// </param>
        /// <param name="rect">the location on the page for the signature field</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfSignatureFormField"/>
        /// </returns>
        public static PdfSignatureFormField CreateSignature(PdfDocument doc, Rectangle rect, PdfAConformanceLevel 
            pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            PdfSignatureFormField signatureFormField = new PdfSignatureFormField(annot, doc);
            signatureFormField.pdfAConformanceLevel = pdfAConformanceLevel;
            if (null != pdfAConformanceLevel) {
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            return signatureFormField;
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField">radio group form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField">radio group</see>
        /// </returns>
        public static PdfButtonFormField CreateRadioGroup(PdfDocument doc, String name, String value) {
            return CreateRadioGroup(doc, name, value, null);
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField">radio group form field</see>
        /// .
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField">radio group</see>
        /// </returns>
        public static PdfButtonFormField CreateRadioGroup(PdfDocument doc, String name, String value, PdfAConformanceLevel
             pdfAConformanceLevel) {
            PdfButtonFormField radio = CreateButton(doc, PdfButtonFormField.FF_RADIO);
            radio.SetFieldName(name);
            radio.Put(PdfName.V, new PdfName(value));
            radio.pdfAConformanceLevel = pdfAConformanceLevel;
            return radio;
        }

        /// <summary>
        /// Creates a generic
        /// <see cref="PdfFormField"/>
        /// that is added to a radio group.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="radioGroup">the radio button group that this field should belong to</param>
        /// <param name="value">the initial value</param>
        /// <returns>
        /// a new
        /// <see cref="PdfFormField"/>
        /// </returns>
        /// <seealso cref="CreateRadioGroup(iText.Kernel.Pdf.PdfDocument, System.String, System.String)"/>
        public static iText.Forms.Fields.PdfFormField CreateRadioButton(PdfDocument doc, Rectangle rect, PdfButtonFormField
             radioGroup, String value) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            iText.Forms.Fields.PdfFormField radio = new PdfButtonFormField(annot, doc);
            String name = radioGroup.GetValue().ToString().Substring(1);
            if (name.Equals(value)) {
                annot.SetAppearanceState(new PdfName(value));
            }
            else {
                annot.SetAppearanceState(new PdfName("Off"));
            }
            radio.DrawRadioAppearance(rect.GetWidth(), rect.GetHeight(), value);
            radioGroup.AddKid(radio);
            return radio;
        }

        /// <summary>
        /// Creates a generic
        /// <see cref="PdfFormField"/>
        /// that is added to a radio group.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="radioGroup">the radio button group that this field should belong to</param>
        /// <param name="value">the initial value</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfFormField"/>
        /// </returns>
        /// <seealso cref="CreateRadioGroup(iText.Kernel.Pdf.PdfDocument, System.String, System.String)"/>
        public static iText.Forms.Fields.PdfFormField CreateRadioButton(PdfDocument doc, Rectangle rect, PdfButtonFormField
             radioGroup, String value, PdfAConformanceLevel pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            iText.Forms.Fields.PdfFormField radio = new PdfButtonFormField(annot, doc);
            radio.pdfAConformanceLevel = pdfAConformanceLevel;
            if (null != pdfAConformanceLevel) {
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            String name = radioGroup.GetValue().ToString().Substring(1);
            if (name.Equals(value)) {
                annot.SetAppearanceState(new PdfName(value));
            }
            else {
                annot.SetAppearanceState(new PdfName("Off"));
            }
            radio.DrawRadioAppearance(rect.GetWidth(), rect.GetHeight(), value);
            radioGroup.AddKid(radio);
            return radio;
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField"/>
        /// as a push button without data.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="caption">the text to display on the button</param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreatePushButton(PdfDocument doc, Rectangle rect, String name, String caption
            ) {
            PdfButtonFormField field;
            try {
                field = CreatePushButton(doc, rect, name, caption, PdfFontFactory.CreateFont(), (float)DEFAULT_FONT_SIZE);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
            return field;
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField"/>
        /// as a push button without data, with
        /// its caption in a custom font.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="caption">the text to display on the button</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreatePushButton(PdfDocument doc, Rectangle rect, String name, String caption
            , PdfFont font, float fontSize) {
            return CreatePushButton(doc, rect, name, caption, font, fontSize, null);
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField"/>
        /// as a push button without data, with
        /// its caption in a custom font.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="caption">the text to display on the button</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField"/>
        /// </returns>
        public static PdfButtonFormField CreatePushButton(PdfDocument doc, Rectangle rect, String name, String caption
            , PdfFont font, float fontSize, PdfAConformanceLevel pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            PdfButtonFormField field = new PdfButtonFormField(annot, doc);
            field.pdfAConformanceLevel = pdfAConformanceLevel;
            if (null != pdfAConformanceLevel) {
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            field.SetPushButton(true);
            field.SetFieldName(name);
            field.text = caption;
            field.font = font;
            field.fontSize = fontSize;
            field.backgroundColor = ColorConstants.LIGHT_GRAY;
            PdfFormXObject xObject = field.DrawPushButtonAppearance(rect.GetWidth(), rect.GetHeight(), caption, font, 
                null, fontSize);
            annot.SetNormalAppearance(xObject.GetPdfObject());
            PdfDictionary mk = new PdfDictionary();
            mk.Put(PdfName.CA, new PdfString(caption));
            mk.Put(PdfName.BG, new PdfArray(field.backgroundColor.GetColorValue()));
            annot.SetAppearanceCharacteristics(mk);
            if (pdfAConformanceLevel != null) {
                CreatePushButtonAppearanceState(annot.GetPdfObject());
            }
            return field;
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField"/>
        /// as a checkbox.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField">checkbox</see>
        /// </returns>
        public static PdfButtonFormField CreateCheckBox(PdfDocument doc, Rectangle rect, String name, String value
            ) {
            return CreateCheckBox(doc, rect, name, value, TYPE_CROSS);
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField"/>
        /// as a checkbox.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="checkType">the type of checkbox graphic to use.</param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField">checkbox</see>
        /// </returns>
        public static PdfButtonFormField CreateCheckBox(PdfDocument doc, Rectangle rect, String name, String value
            , int checkType) {
            return CreateCheckBox(doc, rect, name, value, checkType, null);
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfButtonFormField"/>
        /// as a checkbox.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the radio group in
        /// </param>
        /// <param name="rect">the location on the page for the field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="checkType">the type of checkbox graphic to use.</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfButtonFormField">checkbox</see>
        /// </returns>
        public static PdfButtonFormField CreateCheckBox(PdfDocument doc, Rectangle rect, String name, String value
            , int checkType, PdfAConformanceLevel pdfAConformanceLevel) {
            PdfWidgetAnnotation annot = new PdfWidgetAnnotation(rect);
            PdfButtonFormField check = new PdfButtonFormField(annot, doc);
            check.pdfAConformanceLevel = pdfAConformanceLevel;
            check.SetCheckType(checkType);
            check.SetFieldName(name);
            check.Put(PdfName.V, new PdfName(value));
            annot.SetAppearanceState(new PdfName(value));
            if (pdfAConformanceLevel != null) {
                check.DrawPdfA2CheckAppearance(rect.GetWidth(), rect.GetHeight(), "Off".Equals(value) ? "Yes" : value, checkType
                    );
                annot.SetFlag(PdfAnnotation.PRINT);
            }
            else {
                check.DrawCheckAppearance(rect.GetWidth(), rect.GetHeight(), "Off".Equals(value) ? "Yes" : value);
            }
            return check;
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">combobox</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the combobox in
        /// </param>
        /// <param name="rect">the location on the page for the combobox</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">
        /// a two-dimensional array of Strings which will be converted
        /// to a PdfArray.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a combobox
        /// </returns>
        public static PdfChoiceFormField CreateComboBox(PdfDocument doc, Rectangle rect, String name, String value
            , String[][] options) {
            try {
                return CreateComboBox(doc, rect, name, value, options, PdfFontFactory.CreateFont(), null);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">combobox</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the combobox in
        /// </param>
        /// <param name="rect">the location on the page for the combobox</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">
        /// a two-dimensional array of Strings which will be converted
        /// to a PdfArray.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a combobox
        /// </returns>
        public static PdfChoiceFormField CreateComboBox(PdfDocument doc, Rectangle rect, String name, String value
            , String[][] options, PdfFont font, PdfAConformanceLevel pdfAConformanceLevel) {
            return CreateChoice(doc, rect, name, value, ProcessOptions(options), PdfChoiceFormField.FF_COMBO, font, pdfAConformanceLevel
                );
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">combobox</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the combobox in
        /// </param>
        /// <param name="rect">the location on the page for the combobox</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">an array of Strings which will be converted to a PdfArray.</param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a combobox
        /// </returns>
        public static PdfChoiceFormField CreateComboBox(PdfDocument doc, Rectangle rect, String name, String value
            , String[] options) {
            try {
                return CreateComboBox(doc, rect, name, value, options, PdfFontFactory.CreateFont(), null);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">combobox</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the combobox in
        /// </param>
        /// <param name="rect">the location on the page for the combobox</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">an array of Strings which will be converted to a PdfArray.</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a combobox
        /// </returns>
        public static PdfChoiceFormField CreateComboBox(PdfDocument doc, Rectangle rect, String name, String value
            , String[] options, PdfFont font, PdfAConformanceLevel pdfAConformanceLevel) {
            return CreateChoice(doc, rect, name, value, ProcessOptions(options), PdfChoiceFormField.FF_COMBO, font, pdfAConformanceLevel
                );
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">list field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">
        /// a two-dimensional array of Strings which will be converted
        /// to a PdfArray.
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a list field
        /// </returns>
        public static PdfChoiceFormField CreateList(PdfDocument doc, Rectangle rect, String name, String value, String
            [][] options) {
            try {
                return CreateList(doc, rect, name, value, options, PdfFontFactory.CreateFont(), null);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">list field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the choice field in
        /// </param>
        /// <param name="rect">the location on the page for the choice field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">
        /// a two-dimensional array of Strings which will be converted
        /// to a PdfArray.
        /// </param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a list field
        /// </returns>
        public static PdfChoiceFormField CreateList(PdfDocument doc, Rectangle rect, String name, String value, String
            [][] options, PdfFont font, PdfAConformanceLevel pdfAConformanceLevel) {
            return CreateChoice(doc, rect, name, value, ProcessOptions(options), 0, font, pdfAConformanceLevel);
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">list field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the list field in
        /// </param>
        /// <param name="rect">the location on the page for the list field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">an array of Strings which will be converted to a PdfArray.</param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a list field
        /// </returns>
        public static PdfChoiceFormField CreateList(PdfDocument doc, Rectangle rect, String name, String value, String
            [] options) {
            try {
                return CreateList(doc, rect, name, value, options, PdfFontFactory.CreateFont(), null);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
        }

        /// <summary>
        /// Creates a
        /// <see cref="PdfChoiceFormField">list field</see>
        /// with custom
        /// behavior and layout, on a specified location.
        /// </summary>
        /// <param name="doc">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the list field in
        /// </param>
        /// <param name="rect">the location on the page for the list field</param>
        /// <param name="name">the name of the form field</param>
        /// <param name="value">the initial value</param>
        /// <param name="options">an array of Strings which will be converted to a PdfArray.</param>
        /// <param name="pdfAConformanceLevel">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfAConformanceLevel"/>
        /// of the document.
        /// <c/>
        /// null if it's no PDF/A document
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfChoiceFormField"/>
        /// as a list field
        /// </returns>
        public static PdfChoiceFormField CreateList(PdfDocument doc, Rectangle rect, String name, String value, String
            [] options, PdfFont font, PdfAConformanceLevel pdfAConformanceLevel) {
            return CreateChoice(doc, rect, name, value, ProcessOptions(options), 0, font, pdfAConformanceLevel);
        }

        /// <summary>
        /// Creates a (subtype of)
        /// <see cref="PdfFormField"/>
        /// object. The type of the object
        /// depends on the <code>FT</code> entry in the <code>pdfObject</code> parameter.
        /// </summary>
        /// <param name="pdfObject">
        /// assumed to be either a
        /// <see cref="iText.Kernel.Pdf.PdfDictionary"/>
        /// , or a
        /// <see cref="iText.Kernel.Pdf.PdfIndirectReference"/>
        /// to a
        /// <see cref="iText.Kernel.Pdf.PdfDictionary"/>
        /// </param>
        /// <param name="document">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// to create the field in
        /// </param>
        /// <returns>
        /// a new
        /// <see cref="PdfFormField"/>
        /// , or <code>null</code> if
        /// <code>pdfObject</code> does not contain a <code>FT</code> entry
        /// </returns>
        public static iText.Forms.Fields.PdfFormField MakeFormField(PdfObject pdfObject, PdfDocument document) {
            iText.Forms.Fields.PdfFormField field = null;
            if (pdfObject.IsDictionary()) {
                PdfDictionary dictionary = (PdfDictionary)pdfObject;
                PdfName formType = dictionary.GetAsName(PdfName.FT);
                if (PdfName.Tx.Equals(formType)) {
                    field = new PdfTextFormField(dictionary);
                }
                else {
                    if (PdfName.Btn.Equals(formType)) {
                        field = new PdfButtonFormField(dictionary);
                    }
                    else {
                        if (PdfName.Ch.Equals(formType)) {
                            field = new PdfChoiceFormField(dictionary);
                        }
                        else {
                            if (PdfName.Sig.Equals(formType)) {
                                field = new PdfSignatureFormField(dictionary);
                            }
                            else {
                                field = new iText.Forms.Fields.PdfFormField(dictionary);
                            }
                        }
                    }
                }
            }
            if (field != null) {
                field.MakeIndirect(document);
                if (document != null && document.GetReader() != null && document.GetReader().GetPdfAConformanceLevel() != 
                    null) {
                    field.pdfAConformanceLevel = document.GetReader().GetPdfAConformanceLevel();
                }
            }
            return field;
        }

        /// <summary>
        /// Returns the type of the <p>Parent</p> form field, or of the wrapped
        /// &lt;PdfDictionary&gt; object.
        /// </summary>
        /// <returns>
        /// the form type, as a
        /// <see cref="iText.Kernel.Pdf.PdfName"/>
        /// </returns>
        public virtual PdfName GetFormType() {
            return GetTypeFromParent(GetPdfObject());
        }

        /// <summary>Sets a value to the field and generating field appearance if needed.</summary>
        /// <param name="value">of the field</param>
        /// <returns>the field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetValue(String value) {
            PdfName ft = GetFormType();
            if (ft == null || !ft.Equals(PdfName.Btn)) {
                PdfArray kids = GetKids();
                if (kids != null) {
                    for (int i = 0; i < kids.Size(); i++) {
                        PdfObject kid = kids.Get(i);
                        iText.Forms.Fields.PdfFormField field = new iText.Forms.Fields.PdfFormField((PdfDictionary)kid);
                        field.font = font;
                        field.fontSize = fontSize;
                        field.SetValue(value);
                    }
                }
            }
            return SetValue(value, true);
        }

        /// <summary>Sets a value to the field and generating field appearance if needed.</summary>
        /// <param name="value">of the field</param>
        /// <param name="generateAppearance">set this flat to false if you want to keep the appearance of the field generated before
        ///     </param>
        /// <returns>the field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetValue(String value, bool generateAppearance) {
            PdfName formType = GetFormType();
            if (PdfName.Tx.Equals(formType) || PdfName.Ch.Equals(formType)) {
                Put(PdfName.V, new PdfString(value, PdfEncodings.UNICODE_BIG));
            }
            else {
                if (PdfName.Btn.Equals(formType)) {
                    if ((GetFieldFlags() & PdfButtonFormField.FF_PUSH_BUTTON) != 0) {
                        try {
                            img = ImageDataFactory.Create(Convert.FromBase64String(value));
                        }
                        catch (Exception) {
                            text = value;
                        }
                    }
                    else {
                        Put(PdfName.V, new PdfName(value));
                        foreach (String @as in GetAppearanceStates()) {
                            if (@as.Equals(value)) {
                                Put(PdfName.AS, new PdfName(value));
                                break;
                            }
                        }
                    }
                }
                else {
                    Put(PdfName.V, new PdfString(value, PdfEncodings.UNICODE_BIG));
                }
            }
            if (generateAppearance) {
                RegenerateField();
            }
            this.SetModified();
            return this;
        }

        /// <summary>Set text field value with given font and size</summary>
        /// <param name="value">text value</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetValue(String value, PdfFont font, float fontSize) {
            PdfName formType = GetFormType();
            if (!formType.Equals(PdfName.Tx) && !formType.Equals(PdfName.Ch)) {
                return SetValue(value);
            }
            PdfArray bBox = GetPdfObject().GetAsArray(PdfName.Rect);
            if (bBox == null) {
                PdfArray kids = GetKids();
                if (kids == null) {
                    throw new PdfException(PdfException.WrongFormFieldAddAnnotationToTheField);
                }
                bBox = ((PdfDictionary)kids.Get(0)).GetAsArray(PdfName.Rect);
            }
            PdfFormXObject appearance = new PdfFormXObject(new Rectangle(0, 0, bBox.ToRectangle().GetWidth(), bBox.ToRectangle
                ().GetHeight()));
            if (formType.Equals(PdfName.Tx)) {
                DrawTextAppearance(bBox.ToRectangle(), font, fontSize, value, appearance);
            }
            else {
                //            appearance.getPdfObject().setData(drawTextAppearance(bBox.toRectangle(), font, fontSize, value, appearance));
                DrawMultiLineTextAppearance(bBox.ToRectangle(), font, fontSize, value, appearance);
            }
            //            appearance = drawMultiLineTextAppearance(bBox.toRectangle(), font, fontSize, value, new PdfResources());
            appearance.GetResources().AddFont(GetDocument(), font);
            PdfDictionary ap = new PdfDictionary();
            ap.Put(PdfName.N, appearance.GetPdfObject());
            GetPdfObject().Put(PdfName.V, new PdfString(value, PdfEncodings.UNICODE_BIG));
            return Put(PdfName.AP, ap);
        }

        /// <summary>Sets the field value and the display string.</summary>
        /// <remarks>
        /// Sets the field value and the display string. The display string
        /// is used to build the appearance.
        /// </remarks>
        /// <param name="value">the field value</param>
        /// <param name="display">
        /// the string that is used for the appearance. If <CODE>null</CODE>
        /// the <CODE>value</CODE> parameter will be used
        /// </param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetValue(String value, String display) {
            if (display == null) {
                return SetValue(value);
            }
            SetValue(display, true);
            PdfName formType = GetFormType();
            if (PdfName.Tx.Equals(formType) || PdfName.Ch.Equals(formType)) {
                Put(PdfName.V, new PdfString(value, PdfEncodings.UNICODE_BIG));
            }
            else {
                if (PdfName.Btn.Equals(formType)) {
                    if ((GetFieldFlags() & PdfButtonFormField.FF_PUSH_BUTTON) != 0) {
                        text = value;
                    }
                    else {
                        Put(PdfName.V, new PdfName(value));
                    }
                }
                else {
                    Put(PdfName.V, new PdfString(value, PdfEncodings.UNICODE_BIG));
                }
            }
            return this;
        }

        /// <summary>
        /// Sets a parent
        /// <see cref="PdfFormField"/>
        /// for the current object.
        /// </summary>
        /// <param name="parent">another form field that this field belongs to, usually a group field</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetParent(iText.Forms.Fields.PdfFormField parent) {
            return Put(PdfName.Parent, parent.GetPdfObject());
        }

        /// <summary>Gets the parent dictionary.</summary>
        /// <returns>another form field that this field belongs to, usually a group field</returns>
        public virtual PdfDictionary GetParent() {
            return GetPdfObject().GetAsDictionary(PdfName.Parent);
        }

        /// <summary>Gets the kids of this object.</summary>
        /// <returns>
        /// contents of the dictionary's <code>Kids</code> property, as a
        /// <see cref="iText.Kernel.Pdf.PdfArray"/>
        /// </returns>
        public virtual PdfArray GetKids() {
            return GetPdfObject().GetAsArray(PdfName.Kids);
        }

        /// <summary>
        /// Adds a new kid to the <code>Kids</code> array property from a
        /// <see cref="PdfFormField"/>
        /// . Also sets the kid's <code>Parent</code> property to this object.
        /// </summary>
        /// <param name="kid">
        /// a new
        /// <see cref="PdfFormField"/>
        /// entry for the field's <code>Kids</code> array property
        /// </param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField AddKid(iText.Forms.Fields.PdfFormField kid) {
            kid.SetParent(this);
            PdfArray kids = GetKids();
            if (kids == null) {
                kids = new PdfArray();
            }
            kids.Add(kid.GetPdfObject());
            return Put(PdfName.Kids, kids);
        }

        /// <summary>
        /// Adds a new kid to the <code>Kids</code> array property from a
        /// <see cref="iText.Kernel.Pdf.Annot.PdfWidgetAnnotation"/>
        /// . Also sets the kid's <code>Parent</code> property to this object.
        /// </summary>
        /// <param name="kid">
        /// a new
        /// <see cref="iText.Kernel.Pdf.Annot.PdfWidgetAnnotation"/>
        /// entry for the field's <code>Kids</code> array property
        /// </param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField AddKid(PdfWidgetAnnotation kid) {
            kid.SetParent(GetPdfObject());
            PdfArray kids = GetKids();
            if (kids == null) {
                kids = new PdfArray();
            }
            kids.Add(kid.GetPdfObject());
            return Put(PdfName.Kids, kids);
        }

        /// <summary>Changes the name of the field to the specified value.</summary>
        /// <param name="name">the new field name, as a String</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFieldName(String name) {
            return Put(PdfName.T, new PdfString(name));
        }

        /// <summary>Gets the current field name.</summary>
        /// <returns>
        /// the current field name, as a
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// </returns>
        public virtual PdfString GetFieldName() {
            String parentName = "";
            PdfDictionary parent = GetParent();
            if (parent != null) {
                iText.Forms.Fields.PdfFormField parentField = iText.Forms.Fields.PdfFormField.MakeFormField(GetParent(), GetDocument
                    ());
                PdfString pName = parentField.GetFieldName();
                if (pName != null) {
                    parentName = pName.ToUnicodeString() + ".";
                }
            }
            PdfString name = GetPdfObject().GetAsString(PdfName.T);
            if (name != null) {
                name = new PdfString(parentName + name.ToUnicodeString(), PdfEncodings.UNICODE_BIG);
            }
            return name;
        }

        /// <summary>Changes the alternate name of the field to the specified value.</summary>
        /// <remarks>
        /// Changes the alternate name of the field to the specified value. The
        /// alternate is a descriptive name to be used by status messages etc.
        /// </remarks>
        /// <param name="name">the new alternate name, as a String</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetAlternativeName(String name) {
            return Put(PdfName.TU, new PdfString(name));
        }

        /// <summary>Gets the current alternate name.</summary>
        /// <remarks>
        /// Gets the current alternate name. The alternate is a descriptive name to
        /// be used by status messages etc.
        /// </remarks>
        /// <returns>
        /// the current alternate name, as a
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// </returns>
        public virtual PdfString GetAlternativeName() {
            return GetPdfObject().GetAsString(PdfName.TU);
        }

        /// <summary>Changes the mapping name of the field to the specified value.</summary>
        /// <remarks>
        /// Changes the mapping name of the field to the specified value. The
        /// mapping name can be used when exporting the form data in the document.
        /// </remarks>
        /// <param name="name">the new alternate name, as a String</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetMappingName(String name) {
            return Put(PdfName.TM, new PdfString(name));
        }

        /// <summary>Gets the current mapping name.</summary>
        /// <remarks>
        /// Gets the current mapping name. The mapping name can be used when
        /// exporting the form data in the document.
        /// </remarks>
        /// <returns>
        /// the current mapping name, as a
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// </returns>
        public virtual PdfString GetMappingName() {
            return GetPdfObject().GetAsString(PdfName.TM);
        }

        /// <summary>
        /// Checks whether a certain flag, or any of a combination of flags, is set
        /// for this form field.
        /// </summary>
        /// <param name="flag">an <code>int</code> interpreted as a series of a binary flags</param>
        /// <returns>
        /// true if any of the flags specified in the parameter is also set
        /// in the form field.
        /// </returns>
        public virtual bool GetFieldFlag(int flag) {
            return (GetFieldFlags() & flag) != 0;
        }

        /// <summary>Adds a flag, or combination of flags, for the form field.</summary>
        /// <remarks>
        /// Adds a flag, or combination of flags, for the form field. This method is
        /// intended to be used one flag at a time, but this is not technically
        /// enforced. To <em>replace</em> the current value, use
        /// <see cref="SetFieldFlags(int)"/>
        /// .
        /// </remarks>
        /// <param name="flag">an <code>int</code> interpreted as a series of a binary flags</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFieldFlag(int flag) {
            return SetFieldFlag(flag, true);
        }

        /// <summary>Adds or removes a flag, or combination of flags, for the form field.</summary>
        /// <remarks>
        /// Adds or removes a flag, or combination of flags, for the form field. This
        /// method is intended to be used one flag at a time, but this is not
        /// technically enforced. To <em>replace</em> the current value, use
        /// <see cref="SetFieldFlags(int)"/>
        /// .
        /// </remarks>
        /// <param name="flag">an <code>int</code> interpreted as a series of a binary flags</param>
        /// <param name="value">
        /// if <code>true</code>, adds the flag(s). if <code>false</code>,
        /// removes the flag(s).
        /// </param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFieldFlag(int flag, bool value) {
            int flags = GetFieldFlags();
            if (value) {
                flags |= flag;
            }
            else {
                flags &= ~flag;
            }
            return SetFieldFlags(flags);
        }

        /// <summary>If true, the field can contain multiple lines of text; if false, the field's text is restricted to a single line.
        ///     </summary>
        /// <returns>whether the field can span over multiple lines.</returns>
        public virtual bool IsMultiline() {
            return GetFieldFlag(FF_MULTILINE);
        }

        /// <summary>If true, the field is intended for entering a secure password that should not be echoed visibly to the screen.
        ///     </summary>
        /// <remarks>
        /// If true, the field is intended for entering a secure password that should not be echoed visibly to the screen.
        /// Characters typed from the keyboard should instead be echoed in some unreadable form, such as asterisks or bullet characters.
        /// </remarks>
        /// <returns>whether or not the contents of the field must be obfuscated</returns>
        public virtual bool IsPassword() {
            return GetFieldFlag(FF_PASSWORD);
        }

        /// <summary>Sets a flag, or combination of flags, for the form field.</summary>
        /// <remarks>
        /// Sets a flag, or combination of flags, for the form field. This method
        /// <em>replaces</em> the previous value. Compare with
        /// <see cref="SetFieldFlag(int)"/>
        /// which <em>adds</em> a flag to the existing flags.
        /// </remarks>
        /// <param name="flags">an <code>int</code> interpreted as a series of a binary flags</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFieldFlags(int flags) {
            return Put(PdfName.Ff, new PdfNumber(flags));
        }

        /// <summary>Gets the current list of PDF form field flags.</summary>
        /// <returns>the current list of flags, encoded as an <code>int</code></returns>
        public virtual int GetFieldFlags() {
            PdfNumber f = GetPdfObject().GetAsNumber(PdfName.Ff);
            if (f != null) {
                return f.IntValue();
            }
            else {
                PdfDictionary parent = GetParent();
                if (parent != null) {
                    return new iText.Forms.Fields.PdfFormField(parent).GetFieldFlags();
                }
                else {
                    return 0;
                }
            }
        }

        /// <summary>Gets the current value contained in the form field.</summary>
        /// <returns>
        /// the current value, as a
        /// <see cref="iText.Kernel.Pdf.PdfObject"/>
        /// </returns>
        public virtual PdfObject GetValue() {
            return GetPdfObject().Get(PdfName.V);
        }

        /// <summary>Gets the current value contained in the form field.</summary>
        /// <returns>
        /// the current value, as a
        /// <see cref="System.String"/>
        /// </returns>
        public virtual String GetValueAsString() {
            PdfObject value = GetPdfObject().Get(PdfName.V);
            if (value == null) {
                return "";
            }
            else {
                if (value is PdfStream) {
                    return iText.IO.Util.JavaUtil.GetStringForBytes(((PdfStream)value).GetBytes());
                }
                else {
                    if (value is PdfName) {
                        return ((PdfName)value).GetValue();
                    }
                    else {
                        if (value is PdfString) {
                            return ((PdfString)value).ToUnicodeString();
                        }
                        else {
                            return "";
                        }
                    }
                }
            }
        }

        /// <summary>Sets the default fallback value for the form field.</summary>
        /// <param name="value">the default value</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetDefaultValue(PdfObject value) {
            return Put(PdfName.DV, value);
        }

        /// <summary>Gets the default fallback value for the form field.</summary>
        /// <returns>the default value</returns>
        public virtual PdfObject GetDefaultValue() {
            return GetPdfObject().Get(PdfName.DV);
        }

        /// <summary>Sets an additional action for the form field.</summary>
        /// <param name="key">the dictionary key to use for storing the action</param>
        /// <param name="action">the action</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetAdditionalAction(PdfName key, PdfAction action) {
            PdfAction.SetAdditionalAction(this, key, action);
            return this;
        }

        /// <summary>Gets the currently additional action dictionary for the form field.</summary>
        /// <returns>the additional action dictionary</returns>
        public virtual PdfDictionary GetAdditionalAction() {
            return GetPdfObject().GetAsDictionary(PdfName.AA);
        }

        /// <summary>Sets options for the form field.</summary>
        /// <remarks>Sets options for the form field. Only to be used for checkboxes and radio buttons.</remarks>
        /// <param name="options">
        /// an array of
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// objects that each represent
        /// the 'on' state of one of the choices.
        /// </param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetOptions(PdfArray options) {
            return Put(PdfName.Opt, options);
        }

        /// <summary>Gets options for the form field.</summary>
        /// <remarks>
        /// Gets options for the form field. Should only return usable values for
        /// checkboxes and radio buttons.
        /// </remarks>
        /// <returns>
        /// the options, as an
        /// <see cref="iText.Kernel.Pdf.PdfArray"/>
        /// of
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// objects
        /// </returns>
        public virtual PdfArray GetOptions() {
            return GetPdfObject().GetAsArray(PdfName.Opt);
        }

        /// <summary>
        /// Gets all
        /// <see cref="iText.Kernel.Pdf.Annot.PdfWidgetAnnotation"/>
        /// that this form field and its
        /// <see cref="GetKids()">kids</see>
        /// refer to.
        /// </summary>
        /// <returns>
        /// a list of
        /// <see cref="iText.Kernel.Pdf.Annot.PdfWidgetAnnotation"/>
        /// </returns>
        public virtual IList<PdfWidgetAnnotation> GetWidgets() {
            IList<PdfWidgetAnnotation> widgets = new List<PdfWidgetAnnotation>();
            PdfName subType = GetPdfObject().GetAsName(PdfName.Subtype);
            if (subType != null && subType.Equals(PdfName.Widget)) {
                widgets.Add((PdfWidgetAnnotation)PdfAnnotation.MakeAnnotation(GetPdfObject()));
            }
            PdfArray kids = GetKids();
            if (kids != null) {
                for (int i = 0; i < kids.Size(); i++) {
                    PdfObject kid = kids.Get(i);
                    subType = ((PdfDictionary)kid).GetAsName(PdfName.Subtype);
                    if (subType != null && subType.Equals(PdfName.Widget)) {
                        widgets.Add((PdfWidgetAnnotation)PdfAnnotation.MakeAnnotation(kid));
                    }
                }
            }
            return widgets;
        }

        /// <summary>
        /// Gets default appearance string containing a sequence of valid page-content graphics or text state operators that
        /// define such properties as the field's text size and color.
        /// </summary>
        /// <returns>
        /// the default appearance graphics, as a
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// </returns>
        public virtual PdfString GetDefaultAppearance() {
            PdfString defaultAppearance = GetPdfObject().GetAsString(PdfName.DA);
            if (defaultAppearance == null) {
                PdfDictionary parent = GetParent();
                if (parent != null) {
                    //If this is not merged form field we should get default appearance from the parent which actually is a
                    //form field dictionary
                    if (parent.ContainsKey(PdfName.FT)) {
                        defaultAppearance = parent.GetAsString(PdfName.DA);
                    }
                }
            }
            // DA is an inherited key, therefore AcroForm shall be checked if there is no parent or no DA in parent.
            if (defaultAppearance == null) {
                defaultAppearance = (PdfString)GetAcroFormKey(PdfName.DA, PdfObject.STRING);
            }
            return defaultAppearance;
        }

        /// <summary>
        /// Sets default appearance string containing a sequence of valid page-content graphics or text state operators that
        /// define such properties as the field's text size and color.
        /// </summary>
        /// <param name="defaultAppearance">a valid sequence of PDF content stream syntax</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetDefaultAppearance(String defaultAppearance) {
            byte[] b = defaultAppearance.GetBytes(Encoding.UTF8);
            for (int k = 0; k < b.Length; ++k) {
                if (b[k] == '\n') {
                    b[k] = 32;
                }
            }
            GetPdfObject().Put(PdfName.DA, new PdfString(iText.IO.Util.JavaUtil.GetStringForBytes(b)));
            return this;
        }

        /// <summary>
        /// Gets a code specifying the form of quadding (justification) to be used in displaying the text:
        /// 0 Left-justified
        /// 1 Centered
        /// 2 Right-justified
        /// </summary>
        /// <returns>the current justification attribute</returns>
        public virtual int? GetJustification() {
            int? justification = GetPdfObject().GetAsInt(PdfName.Q);
            if (justification == null && GetParent() != null) {
                justification = GetParent().GetAsInt(PdfName.Q);
            }
            return justification;
        }

        /// <summary>
        /// Sets a code specifying the form of quadding (justification) to be used in displaying the text:
        /// 0 Left-justified
        /// 1 Centered
        /// 2 Right-justified
        /// </summary>
        /// <param name="justification">the value to set the justification attribute to</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetJustification(int justification) {
            GetPdfObject().Put(PdfName.Q, new PdfNumber(justification));
            RegenerateField();
            return this;
        }

        /// <summary>Gets a default style string, as described in "Rich Text Strings" section of Pdf spec.</summary>
        /// <returns>
        /// the default style, as a
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// </returns>
        public virtual PdfString GetDefaultStyle() {
            return GetPdfObject().GetAsString(PdfName.DS);
        }

        /// <summary>Sets a default style string, as described in "Rich Text Strings" section of Pdf spec.</summary>
        /// <param name="defaultStyleString">a new default style for the form field</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetDefaultStyle(PdfString defaultStyleString) {
            GetPdfObject().Put(PdfName.DS, defaultStyleString);
            return this;
        }

        /// <summary>Gets a rich text string, as described in "Rich Text Strings" section of Pdf spec.</summary>
        /// <remarks>
        /// Gets a rich text string, as described in "Rich Text Strings" section of Pdf spec.
        /// May be either
        /// <see cref="iText.Kernel.Pdf.PdfStream"/>
        /// or
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// .
        /// </remarks>
        /// <returns>the current rich text value</returns>
        public virtual PdfObject GetRichText() {
            return GetPdfObject().Get(PdfName.RV);
        }

        /// <summary>Sets a rich text string, as described in "Rich Text Strings" section of Pdf spec.</summary>
        /// <remarks>
        /// Sets a rich text string, as described in "Rich Text Strings" section of Pdf spec.
        /// May be either
        /// <see cref="iText.Kernel.Pdf.PdfStream"/>
        /// or
        /// <see cref="iText.Kernel.Pdf.PdfString"/>
        /// .
        /// </remarks>
        /// <param name="richText">a new rich text value</param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetRichText(PdfObject richText) {
            GetPdfObject().Put(PdfName.RV, richText);
            return this;
        }

        /// <summary>Gets the current font of the form field.</summary>
        /// <returns>
        /// the current
        /// <see cref="iText.Kernel.Font.PdfFont">font</see>
        /// </returns>
        public virtual PdfFont GetFont() {
            return font;
        }

        /// <summary>Basic setter for the <code>font</code> property.</summary>
        /// <remarks>
        /// Basic setter for the <code>font</code> property. Regenerates the field
        /// appearance after setting the new value.
        /// Note that the font will be added to the document so ensure that the font is embedded
        /// if it's a pdf/a document.
        /// </remarks>
        /// <param name="font">The new font to be set</param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFont(PdfFont font) {
            this.font = font;
            RegenerateField();
            return this;
        }

        /// <summary>Basic setter for the <code>fontSize</code> property.</summary>
        /// <remarks>
        /// Basic setter for the <code>fontSize</code> property. Regenerates the
        /// field appearance after setting the new value.
        /// </remarks>
        /// <param name="fontSize">The new font size to be set</param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFontSize(float fontSize) {
            this.fontSize = fontSize;
            RegenerateField();
            return this;
        }

        /// <summary>Basic setter for the <code>fontSize</code> property.</summary>
        /// <remarks>
        /// Basic setter for the <code>fontSize</code> property. Regenerates the
        /// field appearance after setting the new value.
        /// </remarks>
        /// <param name="fontSize">The new font size to be set</param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFontSize(int fontSize) {
            SetFontSize((float)fontSize);
            return this;
        }

        /// <summary>
        /// Combined setter for the <code>font</code> and <code>fontSize</code>
        /// properties.
        /// </summary>
        /// <remarks>
        /// Combined setter for the <code>font</code> and <code>fontSize</code>
        /// properties. Regenerates the field appearance after setting the new value.
        /// </remarks>
        /// <param name="font">The new font to be set</param>
        /// <param name="fontSize">The new font size to be set</param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFontAndSize(PdfFont font, int fontSize) {
            this.font = font;
            this.fontSize = fontSize;
            RegenerateField();
            return this;
        }

        /// <summary>Basic setter for the <code>backgroundColor</code> property.</summary>
        /// <remarks>
        /// Basic setter for the <code>backgroundColor</code> property. Regenerates
        /// the field appearance after setting the new value.
        /// </remarks>
        /// <param name="backgroundColor">
        /// The new color to be set or
        /// <see langword="null"/>
        /// if no background needed
        /// </param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetBackgroundColor(Color backgroundColor) {
            this.backgroundColor = backgroundColor;
            PdfDictionary mk;
            IList<PdfWidgetAnnotation> kids = GetWidgets();
            foreach (PdfWidgetAnnotation kid in kids) {
                mk = kid.GetAppearanceCharacteristics();
                if (mk == null) {
                    mk = new PdfDictionary();
                }
                if (backgroundColor == null) {
                    mk.Remove(PdfName.BG);
                }
                else {
                    mk.Put(PdfName.BG, new PdfArray(backgroundColor.GetColorValue()));
                }
                kid.SetAppearanceCharacteristics(mk);
            }
            RegenerateField();
            return this;
        }

        /// <summary>Basic setter for the <code>degRotation</code> property.</summary>
        /// <remarks>
        /// Basic setter for the <code>degRotation</code> property. Regenerates
        /// the field appearance after setting the new value.
        /// </remarks>
        /// <param name="degRotation">The new degRotation to be set</param>
        /// <returns>The edited PdfFormField</returns>
        public virtual iText.Forms.Fields.PdfFormField SetRotation(int degRotation) {
            if (degRotation % 90 != 0) {
                throw new ArgumentException("degRotation.must.be.a.multiple.of.90");
            }
            else {
                degRotation %= 360;
                if (degRotation < 0) {
                    degRotation += 360;
                }
                this.rotation = degRotation;
            }
            PdfDictionary mk = GetWidgets()[0].GetAppearanceCharacteristics();
            if (mk == null) {
                mk = new PdfDictionary();
                this.Put(PdfName.MK, mk);
            }
            mk.Put(PdfName.R, new PdfNumber(degRotation));
            this.rotation = degRotation;
            RegenerateField();
            return this;
        }

        /// <summary>
        /// Sets the action on all
        /// <see cref="iText.Kernel.Pdf.Annot.PdfWidgetAnnotation">widgets</see>
        /// of this form field.
        /// </summary>
        /// <param name="action">The action</param>
        /// <returns>The edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetAction(PdfAction action) {
            IList<PdfWidgetAnnotation> widgets = GetWidgets();
            if (widgets != null) {
                foreach (PdfWidgetAnnotation widget in widgets) {
                    widget.SetAction(action);
                }
            }
            return this;
        }

        /// <summary>Changes the type of graphical marker used to mark a checkbox as 'on'.</summary>
        /// <remarks>
        /// Changes the type of graphical marker used to mark a checkbox as 'on'.
        /// Notice that in order to complete the change one should call
        /// <see cref="RegenerateField()">regenerateField</see>
        /// method
        /// </remarks>
        /// <param name="checkType">the new checkbox marker</param>
        /// <returns>The edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetCheckType(int checkType) {
            if (checkType < TYPE_CHECK || checkType > TYPE_STAR) {
                checkType = TYPE_CROSS;
            }
            this.checkType = checkType;
            text = typeChars[checkType - 1];
            if (pdfAConformanceLevel != null) {
                return this;
            }
            try {
                font = PdfFontFactory.CreateFont(StandardFonts.ZAPFDINGBATS);
            }
            catch (System.IO.IOException e) {
                throw new PdfException(e);
            }
            return this;
        }

        /// <summary>
        /// Set the visibility flags of the form field annotation
        /// Options are: HIDDEN, HIDDEN_BUT_PRINTABLE, VISIBLE, VISIBLE_BUT_DOES_NOT_PRINT
        /// </summary>
        /// <param name="visibility">visibility option</param>
        /// <returns>The edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetVisibility(int visibility) {
            switch (visibility) {
                case HIDDEN: {
                    GetPdfObject().Put(PdfName.F, new PdfNumber(PdfAnnotation.PRINT | PdfAnnotation.HIDDEN));
                    break;
                }

                case VISIBLE_BUT_DOES_NOT_PRINT: {
                    break;
                }

                case HIDDEN_BUT_PRINTABLE: {
                    GetPdfObject().Put(PdfName.F, new PdfNumber(PdfAnnotation.PRINT | PdfAnnotation.NO_VIEW));
                    break;
                }

                case VISIBLE:
                default: {
                    GetPdfObject().Put(PdfName.F, new PdfNumber(PdfAnnotation.PRINT));
                    break;
                }
            }
            return this;
        }

        /// <summary>This method regenerates appearance stream of the field.</summary>
        /// <remarks>
        /// This method regenerates appearance stream of the field. Use it if you
        /// changed any field parameters and didn't use setValue method which
        /// generates appearance by itself.
        /// </remarks>
        /// <returns>whether or not the regeneration was successful.</returns>
        public virtual bool RegenerateField() {
            PdfName type = GetFormType();
            String value = GetValueAsString();
            PdfPage page = null;
            if (GetWidgets().Count > 0) {
                page = GetWidgets()[0].GetPage();
            }
            if (PdfName.Tx.Equals(type) || PdfName.Ch.Equals(type)) {
                try {
                    PdfDictionary apDic = GetPdfObject().GetAsDictionary(PdfName.AP);
                    PdfStream asNormal = null;
                    if (apDic != null) {
                        //TODO DEVSIX-2528 what if PdfName.N is PdfDictionary?
                        asNormal = apDic.GetAsStream(PdfName.N);
                    }
                    PdfArray bBox = GetPdfObject().GetAsArray(PdfName.Rect);
                    if (bBox == null) {
                        PdfArray kids = GetKids();
                        if (kids == null) {
                            throw new PdfException(PdfException.WrongFormFieldAddAnnotationToTheField);
                        }
                        bBox = ((PdfDictionary)kids.Get(0)).GetAsArray(PdfName.Rect);
                    }
                    Object[] fontAndSize = GetFontAndSize(asNormal);
                    PdfFont localFont = (PdfFont)fontAndSize[0];
                    PdfName localFontName = (PdfName)fontAndSize[2];
                    float fontSize = NormalizeFontSize((float)fontAndSize[1], localFont, bBox, value);
                    //Apply Page rotation
                    int pageRotation = 0;
                    if (page != null) {
                        pageRotation = page.GetRotation();
                        //Clockwise, so negative
                        pageRotation *= -1;
                    }
                    PdfArray matrix;
                    if (pageRotation % 90 == 0) {
                        //Cast angle to [-360, 360]
                        double angle = pageRotation % 360;
                        //Get angle in radians
                        angle = DegreeToRadians(angle);
                        //rotate the bounding box
                        Rectangle rect = bBox.ToRectangle();
                        //Calculate origin offset
                        double translationWidth = 0;
                        double translationHeight = 0;
                        if (angle >= -1 * Math.PI && angle <= -1 * Math.PI / 2) {
                            translationWidth = rect.GetWidth();
                        }
                        if (angle <= -1 * Math.PI) {
                            translationHeight = rect.GetHeight();
                        }
                        //Store rotation and translation in the matrix
                        matrix = new PdfArray(new double[] { Math.Cos(angle), -Math.Sin(angle), Math.Sin(angle), Math.Cos(angle), 
                            translationWidth, translationHeight });
                        //If the angle is a multiple of 90 and not a multiple of 180, height and width of the bounding box need to be switched
                        if (angle % (Math.PI / 2) == 0 && angle % (Math.PI) != 0) {
                            rect.SetWidth(bBox.ToRectangle().GetHeight());
                            rect.SetHeight(bBox.ToRectangle().GetWidth());
                        }
                        // Adapt origin
                        rect.SetX(rect.GetX() + (float)translationWidth);
                        rect.SetY(rect.GetY() + (float)translationHeight);
                        //Copy Bounding box
                        bBox = new PdfArray(rect);
                    }
                    else {
                        //Avoid NPE when handling corrupt pdfs
                        ILog logger = LogManager.GetLogger(typeof(iText.Forms.Fields.PdfFormField));
                        logger.Error(iText.IO.LogMessageConstant.INCORRECT_PAGEROTATION);
                        matrix = new PdfArray(new double[] { 1, 0, 0, 1, 0, 0 });
                    }
                    //Apply field rotation
                    float fieldRotation = 0;
                    if (this.GetPdfObject().GetAsDictionary(PdfName.MK) != null && this.GetPdfObject().GetAsDictionary(PdfName
                        .MK).Get(PdfName.R) != null) {
                        fieldRotation = (float)this.GetPdfObject().GetAsDictionary(PdfName.MK).GetAsFloat(PdfName.R);
                        //Get relative field rotation
                        fieldRotation += pageRotation;
                    }
                    if (fieldRotation % 90 == 0) {
                        //Cast angle to [-360, 360]
                        double angle = fieldRotation % 360;
                        //Get angle in radians
                        angle = DegreeToRadians(angle);
                        //Calculate origin offset
                        double translationWidth = CalculateTranslationWidthAfterFieldRot(bBox.ToRectangle(), DegreeToRadians(pageRotation
                            ), angle);
                        double translationHeight = CalculateTranslationHeightAfterFieldRot(bBox.ToRectangle(), DegreeToRadians(pageRotation
                            ), angle);
                        //Concatenate rotation and translation into the matrix
                        Matrix currentMatrix = new Matrix(matrix.GetAsNumber(0).FloatValue(), matrix.GetAsNumber(1).FloatValue(), 
                            matrix.GetAsNumber(2).FloatValue(), matrix.GetAsNumber(3).FloatValue(), matrix.GetAsNumber(4).FloatValue
                            (), matrix.GetAsNumber(5).FloatValue());
                        Matrix toConcatenate = new Matrix((float)Math.Cos(angle), (float)(-Math.Sin(angle)), (float)(Math.Sin(angle
                            )), (float)(Math.Cos(angle)), (float)translationWidth, (float)translationHeight);
                        currentMatrix = currentMatrix.Multiply(toConcatenate);
                        matrix = new PdfArray(new float[] { currentMatrix.Get(0), currentMatrix.Get(1), currentMatrix.Get(3), currentMatrix
                            .Get(4), currentMatrix.Get(6), currentMatrix.Get(7) });
                        //Construct bounding box
                        Rectangle rect = bBox.ToRectangle();
                        //If the angle is a multiple of 90 and not a multiple of 180, height and width of the bounding box need to be switched
                        if (angle % (Math.PI / 2) == 0 && angle % (Math.PI) != 0) {
                            rect.SetWidth(bBox.ToRectangle().GetHeight());
                            rect.SetHeight(bBox.ToRectangle().GetWidth());
                        }
                        rect.SetX(rect.GetX() + (float)translationWidth);
                        rect.SetY(rect.GetY() + (float)translationHeight);
                        //Copy Bounding box
                        bBox = new PdfArray(rect);
                    }
                    //Create appearance
                    AppearanceXObject appearance = new AppearanceXObject(new Rectangle(0, 0, bBox.ToRectangle().GetWidth(), bBox
                        .ToRectangle().GetHeight()));
                    appearance.AddFontFromDR(localFontName, localFont);
                    appearance.Put(PdfName.Matrix, matrix);
                    //Create text appearance
                    if (PdfName.Tx.Equals(type)) {
                        if (!IsMultiline()) {
                            DrawTextAppearance(bBox.ToRectangle(), localFont, fontSize, value, appearance);
                        }
                        else {
                            DrawMultiLineTextAppearance(bBox.ToRectangle(), localFont, fontSize, value, appearance);
                        }
                    }
                    else {
                        if (!GetFieldFlag(PdfChoiceFormField.FF_COMBO)) {
                            PdfNumber topIndex = this.GetPdfObject().GetAsNumber(PdfName.TI);
                            PdfArray options = GetOptions();
                            if (null != options) {
                                PdfArray visibleOptions = null != topIndex ? new PdfArray(options.SubList(topIndex.IntValue(), options.Size
                                    () - 1)) : (PdfArray)options.Clone();
                                value = OptionsArrayToString(visibleOptions);
                            }
                        }
                        DrawMultiLineTextAppearance(bBox.ToRectangle(), localFont, fontSize, value, appearance);
                    }
                    appearance.GetResources().AddFont(GetDocument(), localFont);
                    appearance.SetModified();
                    PdfDictionary ap = new PdfDictionary();
                    ap.Put(PdfName.N, appearance.GetPdfObject());
                    ap.SetModified();
                    Put(PdfName.AP, ap);
                    return true;
                }
                catch (System.IO.IOException e) {
                    throw new PdfException(e);
                }
            }
            else {
                if (PdfName.Btn.Equals(type)) {
                    int ff = GetFieldFlags();
                    if ((ff & PdfButtonFormField.FF_PUSH_BUTTON) != 0) {
                        try {
                            value = text;
                            PdfDictionary widget = GetPdfObject();
                            PdfFormXObject appearance;
                            Rectangle rect = GetRect(GetPdfObject());
                            PdfDictionary apDic = GetPdfObject().GetAsDictionary(PdfName.AP);
                            if (apDic == null) {
                                IList<PdfWidgetAnnotation> widgets = GetWidgets();
                                if (widgets.Count == 1) {
                                    widget = widgets[0].GetPdfObject();
                                    apDic = widget.GetAsDictionary(PdfName.AP);
                                }
                            }
                            if (apDic == null) {
                                Put(PdfName.AP, apDic = new PdfDictionary());
                                widget = GetPdfObject();
                            }
                            if (img != null || form != null) {
                                appearance = DrawPushButtonAppearance(rect.GetWidth(), rect.GetHeight(), value, null, null, 0);
                            }
                            else {
                                //TODO DEVSIX-2528 what if PdfName.N is PdfDictionary?
                                Object[] fontAndSize = GetFontAndSize(apDic.GetAsStream(PdfName.N));
                                PdfFont localFont = (PdfFont)fontAndSize[0];
                                PdfName localFontName = (PdfName)fontAndSize[2];
                                float fontSize = (float)fontAndSize[1];
                                appearance = DrawPushButtonAppearance(rect.GetWidth(), rect.GetHeight(), value, localFont, localFontName, 
                                    fontSize);
                            }
                            apDic.Put(PdfName.N, appearance.GetPdfObject());
                            if (pdfAConformanceLevel != null) {
                                CreatePushButtonAppearanceState(widget);
                            }
                        }
                        catch (System.IO.IOException e) {
                            throw new PdfException(e);
                        }
                    }
                    else {
                        if ((ff & PdfButtonFormField.FF_RADIO) != 0) {
                            if (IsRadioButton()) {
                                // TODO DEVSIX-2536
                                // Actually only radio group has FF_RADIO type.
                                // This means that only radio group shall have regeneration functionality.
                                Rectangle rect = GetRect(GetPdfObject());
                                value = GetRadioButtonValue(value);
                                if (rect != null && !"".Equals(value)) {
                                    DrawRadioAppearance(rect.GetWidth(), rect.GetHeight(), value);
                                }
                            }
                            else {
                                if (GetKids() != null) {
                                    foreach (PdfObject kid in GetKids()) {
                                        iText.Forms.Fields.PdfFormField field = new iText.Forms.Fields.PdfFormField((PdfDictionary)kid);
                                        PdfWidgetAnnotation widget = field.GetWidgets()[0];
                                        PdfDictionary apStream = field.GetPdfObject().GetAsDictionary(PdfName.AP);
                                        if (apStream == null) {
                                            //widget annotation was not merged
                                            apStream = widget.GetPdfObject().GetAsDictionary(PdfName.AP);
                                        }
                                        PdfName state;
                                        if (null != apStream && null != GetValueFromAppearance(apStream.Get(PdfName.N), new PdfName(value))) {
                                            state = new PdfName(value);
                                        }
                                        else {
                                            state = new PdfName("Off");
                                        }
                                        widget.SetAppearanceState(state);
                                    }
                                }
                            }
                        }
                        else {
                            Rectangle rect = GetRect(GetPdfObject());
                            SetCheckType(checkType);
                            PdfWidgetAnnotation widget = GetWidgets()[0];
                            if (pdfAConformanceLevel != null) {
                                DrawPdfA2CheckAppearance(rect.GetWidth(), rect.GetHeight(), "Off".Equals(value) ? "Yes" : value, checkType
                                    );
                                widget.SetFlag(PdfAnnotation.PRINT);
                            }
                            else {
                                DrawCheckAppearance(rect.GetWidth(), rect.GetHeight(), "Off".Equals(value) ? "Yes" : value);
                            }
                            if (widget.GetNormalAppearanceObject() != null && widget.GetNormalAppearanceObject().ContainsKey(new PdfName
                                (value))) {
                                widget.SetAppearanceState(new PdfName(value));
                            }
                            else {
                                widget.SetAppearanceState(new PdfName("Off"));
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static void CreatePushButtonAppearanceState(PdfDictionary widget) {
            PdfDictionary appearances = widget.GetAsDictionary(PdfName.AP);
            PdfStream normalAppearanceStream = appearances.GetAsStream(PdfName.N);
            if (normalAppearanceStream != null) {
                PdfName stateName = widget.GetAsName(PdfName.AS);
                if (stateName == null) {
                    stateName = new PdfName("push");
                }
                widget.Put(PdfName.AS, stateName);
                PdfDictionary normalAppearance = new PdfDictionary();
                normalAppearance.Put(stateName, normalAppearanceStream);
                appearances.Put(PdfName.N, normalAppearance);
            }
        }

        // TODO DEVSIX-2536
        // Actually this entire method is a mess,
        // because only radio group has FF_RADIO type and there is no RadioButton at all.
        // So the goal of that method is just to save backward compatibility until refactoring.
        private bool IsRadioButton() {
            if (IsWidgetAnnotation(GetPdfObject())) {
                return true;
            }
            else {
                if (GetPdfObject().GetAsName(PdfName.V) != null) {
                    return false;
                }
                else {
                    if (GetKids() != null) {
                        return IsWidgetAnnotation(GetKids().GetAsDictionary(0));
                    }
                    else {
                        return false;
                    }
                }
            }
        }

        private static bool IsWidgetAnnotation(PdfDictionary pdfObject) {
            return pdfObject != null && PdfName.Widget.Equals(pdfObject.GetAsName(PdfName.Subtype));
        }

        private String GetRadioButtonValue(String value) {
            System.Diagnostics.Debug.Assert(value != null);
            //Otherwise something wrong with getValueAsString().
            if ("".Equals(value)) {
                value = "Yes";
                //let it as default value
                foreach (String state in GetAppearanceStates()) {
                    if (!"Off".Equals(state)) {
                        value = state;
                        break;
                    }
                }
            }
            return value;
        }

        /// <summary>According to spec (ISO-32000-1, 12.7.3.3) zero font size should interpretaded as auto size.</summary>
        private float NormalizeFontSize(float fs, PdfFont localFont, PdfArray bBox, String value) {
            if (fs == 0) {
                if (IsMultiline()) {
                    //We do not support autosize with multiline.
                    fontSize = DEFAULT_FONT_SIZE;
                }
                else {
                    // Save it for Default Appearance.
                    fontSize = 0;
                    fs = ApproximateFontSizeToFitBBox(localFont, bBox.ToRectangle(), value);
                }
            }
            if (fs < MIN_FONT_SIZE) {
                fs = MIN_FONT_SIZE;
            }
            return fs;
        }

        private float ApproximateFontSizeToFitBBox(PdfFont localFont, Rectangle bBox, String value) {
            float fs;
            float height = bBox.GetHeight() - borderWidth * 2;
            int[] fontBbox = localFont.GetFontProgram().GetFontMetrics().GetBbox();
            fs = height / (fontBbox[2] - fontBbox[1]) * FontProgram.UNITS_NORMALIZATION;
            float baseWidth = localFont.GetWidth(value, 1);
            if (baseWidth != 0) {
                float availableWidth = Math.Max(bBox.GetWidth() - borderWidth * 2, 0);
                // This constant is taken based on what was the resultant padding in previous version of this algorithm in case border width was zero.
                float absMaxPadding = 4f;
                // relative value is quite big in order to preserve visible padding on small field sizes. This constant is taken arbitrary, based on visual similarity to Acrobat behaviour.
                float relativePaddingForSmallSizes = 0.15f;
                // with current constants, if availableWidth is less than ~26 points, padding will be made relative
                if (availableWidth * relativePaddingForSmallSizes < absMaxPadding) {
                    availableWidth -= availableWidth * relativePaddingForSmallSizes * 2;
                }
                else {
                    availableWidth -= absMaxPadding * 2;
                }
                fs = Math.Min(fs, availableWidth / baseWidth);
            }
            return fs;
        }

        /// <summary>
        /// Calculate the necessary height offset after applying field rotation
        /// so that the origin of the bounding box is the lower left corner with respect to the field text.
        /// </summary>
        /// <param name="bBox">bounding box rectangle before rotation</param>
        /// <param name="pageRotation">rotation of the page</param>
        /// <param name="relFieldRotation">rotation of the field relative to the page</param>
        /// <returns>translation value for height</returns>
        private float CalculateTranslationHeightAfterFieldRot(Rectangle bBox, double pageRotation, double relFieldRotation
            ) {
            if (relFieldRotation == 0) {
                return 0.0f;
            }
            if (pageRotation == 0) {
                if (relFieldRotation == Math.PI / 2) {
                    return bBox.GetHeight();
                }
                if (relFieldRotation == Math.PI) {
                    return bBox.GetHeight();
                }
            }
            if (pageRotation == -Math.PI / 2) {
                if (relFieldRotation == -Math.PI / 2) {
                    return bBox.GetWidth() - bBox.GetHeight();
                }
                if (relFieldRotation == Math.PI / 2) {
                    return bBox.GetHeight();
                }
                if (relFieldRotation == Math.PI) {
                    return bBox.GetWidth();
                }
            }
            if (pageRotation == -Math.PI) {
                if (relFieldRotation == -1 * Math.PI) {
                    return bBox.GetHeight();
                }
                if (relFieldRotation == -1 * Math.PI / 2) {
                    return bBox.GetHeight() - bBox.GetWidth();
                }
                if (relFieldRotation == Math.PI / 2) {
                    return bBox.GetWidth();
                }
            }
            if (pageRotation == -3 * Math.PI / 2) {
                if (relFieldRotation == -3 * Math.PI / 2) {
                    return bBox.GetWidth();
                }
                if (relFieldRotation == -Math.PI) {
                    return bBox.GetWidth();
                }
            }
            return 0.0f;
        }

        /// <summary>
        /// Calculate the necessary width offset after applying field rotation
        /// so that the origin of the bounding box is the lower left corner with respect to the field text.
        /// </summary>
        /// <param name="bBox">bounding box rectangle before rotation</param>
        /// <param name="pageRotation">rotation of the page</param>
        /// <param name="relFieldRotation">rotation of the field relative to the page</param>
        /// <returns>translation value for width</returns>
        private float CalculateTranslationWidthAfterFieldRot(Rectangle bBox, double pageRotation, double relFieldRotation
            ) {
            if (relFieldRotation == 0) {
                return 0.0f;
            }
            if (pageRotation == 0 && (relFieldRotation == Math.PI || relFieldRotation == 3 * Math.PI / 2)) {
                return bBox.GetWidth();
            }
            if (pageRotation == -Math.PI / 2) {
                if (relFieldRotation == -Math.PI / 2 || relFieldRotation == Math.PI) {
                    return bBox.GetHeight();
                }
            }
            if (pageRotation == -Math.PI) {
                if (relFieldRotation == -1 * Math.PI) {
                    return bBox.GetWidth();
                }
                if (relFieldRotation == -1 * Math.PI / 2) {
                    return bBox.GetHeight();
                }
                if (relFieldRotation == Math.PI / 2) {
                    return -1 * (bBox.GetHeight() - bBox.GetWidth());
                }
            }
            if (pageRotation == -3 * Math.PI / 2) {
                if (relFieldRotation == -3 * Math.PI / 2) {
                    return -1 * (bBox.GetWidth() - bBox.GetHeight());
                }
                if (relFieldRotation == -Math.PI) {
                    return bBox.GetHeight();
                }
                if (relFieldRotation == -Math.PI / 2) {
                    return bBox.GetWidth();
                }
            }
            return 0.0f;
        }

        /// <summary>Gets the border width for the field.</summary>
        /// <returns>the current border width.</returns>
        public virtual float GetBorderWidth() {
            PdfDictionary bs = GetWidgets()[0].GetBorderStyle();
            if (bs != null) {
                PdfNumber w = bs.GetAsNumber(PdfName.W);
                if (w != null) {
                    borderWidth = w.FloatValue();
                }
            }
            return borderWidth;
        }

        /// <summary>Sets the border width for the field.</summary>
        /// <param name="borderWidth">The new border width.</param>
        /// <returns>The edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetBorderWidth(float borderWidth) {
            PdfDictionary bs = GetWidgets()[0].GetBorderStyle();
            if (bs == null) {
                bs = new PdfDictionary();
                Put(PdfName.BS, bs);
            }
            bs.Put(PdfName.W, new PdfNumber(borderWidth));
            this.borderWidth = borderWidth;
            RegenerateField();
            return this;
        }

        public virtual iText.Forms.Fields.PdfFormField SetBorderStyle(PdfDictionary style) {
            //PdfDictionary bs = getWidgets().get(0).getBorderStyle();
            GetWidgets()[0].SetBorderStyle(style);
            //        if (bs == null) {
            //            bs = new PdfDictionary();
            //            put(PdfName.BS, bs);
            //        }
            //        bs.put(PdfName.S, style);
            RegenerateField();
            return this;
        }

        /// <summary>Sets the Border Color.</summary>
        /// <param name="color">the new value for the Border Color</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetBorderColor(Color color) {
            borderColor = color;
            PdfDictionary mk;
            IList<PdfWidgetAnnotation> kids = GetWidgets();
            foreach (PdfWidgetAnnotation kid in kids) {
                mk = kid.GetAppearanceCharacteristics();
                if (mk == null) {
                    mk = new PdfDictionary();
                }
                if (borderColor == null) {
                    mk.Remove(PdfName.BC);
                }
                else {
                    mk.Put(PdfName.BC, new PdfArray(borderColor.GetColorValue()));
                }
                kid.SetAppearanceCharacteristics(mk);
            }
            RegenerateField();
            return this;
        }

        /// <summary>Sets the text color.</summary>
        /// <param name="color">the new value for the Color</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetColor(Color color) {
            this.color = color;
            RegenerateField();
            return this;
        }

        /// <summary>Sets the ReadOnly flag, specifying whether or not the field can be changed.</summary>
        /// <param name="readOnly">if <code>true</code>, then the field cannot be changed.</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetReadOnly(bool readOnly) {
            return SetFieldFlag(FF_READ_ONLY, readOnly);
        }

        /// <summary>Gets the ReadOnly flag, specifying whether or not the field can be changed.</summary>
        /// <returns><code>true</code> if the field cannot be changed.</returns>
        public virtual bool IsReadOnly() {
            return GetFieldFlag(FF_READ_ONLY);
        }

        /// <summary>Sets the Required flag, specifying whether or not the field must be filled in.</summary>
        /// <param name="required">if <code>true</code>, then the field must be filled in.</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetRequired(bool required) {
            return SetFieldFlag(FF_REQUIRED, required);
        }

        /// <summary>Gets the Required flag, specifying whether or not the field must be filled in.</summary>
        /// <returns><code>true</code> if the field must be filled in.</returns>
        public virtual bool IsRequired() {
            return GetFieldFlag(FF_REQUIRED);
        }

        /// <summary>Sets the NoExport flag, specifying whether or not exporting is forbidden.</summary>
        /// <param name="noExport">if <code>true</code>, then exporting is <em>forbidden</em></param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetNoExport(bool noExport) {
            return SetFieldFlag(FF_NO_EXPORT, noExport);
        }

        /// <summary>Gets the NoExport attribute.</summary>
        /// <returns>whether exporting the value following a form action is forbidden.</returns>
        public virtual bool IsNoExport() {
            return GetFieldFlag(FF_NO_EXPORT);
        }

        /// <summary>Specifies on which page the form field's widget must be shown.</summary>
        /// <param name="pageNum">the page number</param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetPage(int pageNum) {
            if (GetWidgets().Count > 0) {
                PdfAnnotation annot = GetWidgets()[0];
                if (annot != null) {
                    annot.SetPage(GetDocument().GetPage(pageNum));
                }
            }
            return this;
        }

        /// <summary>Gets the appearance state names.</summary>
        /// <returns>an array of Strings containing the names of the appearance states</returns>
        public virtual String[] GetAppearanceStates() {
            ICollection<String> names = new LinkedHashSet<String>();
            PdfString stringOpt = GetPdfObject().GetAsString(PdfName.Opt);
            if (stringOpt != null) {
                names.Add(stringOpt.ToUnicodeString());
            }
            else {
                PdfArray arrayOpt = GetPdfObject().GetAsArray(PdfName.Opt);
                if (arrayOpt != null) {
                    foreach (PdfObject pdfObject in arrayOpt) {
                        PdfString valStr = null;
                        if (pdfObject.IsArray()) {
                            valStr = ((PdfArray)pdfObject).GetAsString(1);
                        }
                        else {
                            if (pdfObject.IsString()) {
                                valStr = (PdfString)pdfObject;
                            }
                        }
                        if (valStr != null) {
                            names.Add(valStr.ToUnicodeString());
                        }
                    }
                }
            }
            PdfDictionary dic = GetPdfObject();
            dic = dic.GetAsDictionary(PdfName.AP);
            if (dic != null) {
                //TODO DEVSIX-2528 what if PdfName.N is PdfDictionary?
                dic = dic.GetAsDictionary(PdfName.N);
                if (dic != null) {
                    foreach (PdfName state in dic.KeySet()) {
                        names.Add(state.GetValue());
                    }
                }
            }
            PdfArray kids = GetKids();
            if (kids != null) {
                foreach (PdfObject kid in kids) {
                    iText.Forms.Fields.PdfFormField fld = new iText.Forms.Fields.PdfFormField((PdfDictionary)kid);
                    String[] states = fld.GetAppearanceStates();
                    foreach (String state in states) {
                        names.Add(state);
                    }
                }
            }
            return names.ToArray(new String[names.Count]);
        }

        /// <summary>Sets an appearance for (the widgets related to) the form field.</summary>
        /// <param name="appearanceType">
        /// the type of appearance stream to be added
        /// <ul>
        /// <li> PdfName.N: normal appearance</li>
        /// <li> PdfName.R: rollover appearance</li>
        /// <li> PdfName.D: down appearance</li>
        /// </ul>
        /// </param>
        /// <param name="appearanceState">
        /// the state of the form field that needs to be true
        /// for the appearance to be used. Differentiates between several streams
        /// of the same type.
        /// </param>
        /// <param name="appearanceStream">
        /// the appearance instructions, as a
        /// <see cref="iText.Kernel.Pdf.PdfStream"/>
        /// </param>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetAppearance(PdfName appearanceType, String appearanceState
            , PdfStream appearanceStream) {
            PdfWidgetAnnotation widget = GetWidgets()[0];
            PdfDictionary dic;
            if (widget != null) {
                dic = widget.GetPdfObject();
            }
            else {
                dic = GetPdfObject();
            }
            PdfDictionary ap = dic.GetAsDictionary(PdfName.AP);
            if (ap != null) {
                PdfDictionary appearanceDictionary = ap.GetAsDictionary(appearanceType);
                if (appearanceDictionary == null) {
                    ap.Put(appearanceType, appearanceStream);
                }
                else {
                    appearanceDictionary.Put(new PdfName(appearanceState), appearanceStream);
                }
            }
            return this;
        }

        /// <summary>Sets zero font size which will be interpreted as auto-size according to ISO 32000-1, 12.7.3.3.</summary>
        /// <returns>the edited field</returns>
        public virtual iText.Forms.Fields.PdfFormField SetFontSizeAutoScale() {
            this.fontSize = 0;
            RegenerateField();
            return this;
        }

        public virtual iText.Forms.Fields.PdfFormField Put(PdfName key, PdfObject value) {
            GetPdfObject().Put(key, value);
            return this;
        }

        /// <summary>Releases underlying pdf object and other pdf entities used by wrapper.</summary>
        /// <remarks>
        /// Releases underlying pdf object and other pdf entities used by wrapper.
        /// This method should be called instead of direct call to
        /// <see cref="iText.Kernel.Pdf.PdfObject.Release()"/>
        /// if the wrapper is used.
        /// </remarks>
        public virtual void Release() {
            UnsetForbidRelease();
            GetPdfObject().Release();
        }

        protected override bool IsWrappedObjectMustBeIndirect() {
            return true;
        }

        protected internal virtual PdfDocument GetDocument() {
            return GetPdfObject().GetIndirectReference().GetDocument();
        }

        protected internal virtual Rectangle GetRect(PdfDictionary field) {
            PdfArray rect = field.GetAsArray(PdfName.Rect);
            if (rect == null) {
                PdfArray kids = field.GetAsArray(PdfName.Kids);
                if (kids == null) {
                    throw new PdfException(PdfException.WrongFormFieldAddAnnotationToTheField);
                }
                rect = ((PdfDictionary)kids.Get(0)).GetAsArray(PdfName.Rect);
            }
            return rect != null ? rect.ToRectangle() : null;
        }

        protected internal static PdfArray ProcessOptions(String[][] options) {
            PdfArray array = new PdfArray();
            foreach (String[] option in options) {
                PdfArray subArray = new PdfArray(new PdfString(option[0], PdfEncodings.UNICODE_BIG));
                subArray.Add(new PdfString(option[1], PdfEncodings.UNICODE_BIG));
                array.Add(subArray);
            }
            return array;
        }

        protected internal static PdfArray ProcessOptions(String[] options) {
            PdfArray array = new PdfArray();
            foreach (String option in options) {
                array.Add(new PdfString(option, PdfEncodings.UNICODE_BIG));
            }
            return array;
        }

        /// <summary>Generate default appearance, /DA key.</summary>
        /// <param name="font">
        /// preferred font. If
        /// <see cref="GetFont()"/>
        /// is not null, it will be used instead.
        /// </param>
        /// <param name="fontSize">
        /// preferred font size. If
        /// <see cref="fontSize"/>
        /// is valid,
        /// it will be used instead.
        /// </param>
        /// <returns>generated string</returns>
        protected internal virtual String GenerateDefaultAppearanceString(PdfFont font, float fontSize, Color color
            , PdfResources res) {
            if (this.fontSize >= 0) {
                fontSize = this.fontSize;
            }
            if (this.font != null) {
                font = this.font;
            }
            PdfStream stream = new PdfStream();
            PdfCanvas canvas = new PdfCanvas(stream, res, GetDocument());
            canvas.SetFontAndSize(font, fontSize);
            if (color != null) {
                canvas.SetColor(color, true);
            }
            return iText.IO.Util.JavaUtil.GetStringForBytes(stream.GetBytes());
        }

        /// <exception cref="System.IO.IOException"/>
        protected internal virtual Object[] GetFontAndSize(PdfDictionary asNormal) {
            Object[] fontAndSize = new Object[3];
            PdfDictionary normalResources = null;
            PdfDictionary defaultResources = null;
            PdfDocument document = GetDocument();
            defaultResources = (PdfDictionary)GetAcroFormKey(PdfName.DR, PdfObject.DICTIONARY);
            if (asNormal != null) {
                normalResources = asNormal.GetAsDictionary(PdfName.Resources);
            }
            PdfDictionary daFontDict = null;
            PdfName daFontName = null;
            Object[] dab = new Object[3];
            if (defaultResources != null || normalResources != null) {
                PdfDictionary normalFontDic = normalResources != null ? normalResources.GetAsDictionary(PdfName.Font) : null;
                PdfDictionary defaultFontDic = defaultResources != null ? defaultResources.GetAsDictionary(PdfName.Font) : 
                    null;
                PdfString defaultAppearance = GetDefaultAppearance();
                if ((normalFontDic != null || defaultFontDic != null) && defaultAppearance != null) {
                    dab = SplitDAelements(defaultAppearance.ToUnicodeString());
                    Object fontNameObj = dab[DA_FONT];
                    if (fontNameObj != null) {
                        daFontName = new PdfName(fontNameObj.ToString());
                        // according to spec, DA font shall be taken from the DR
                        if (defaultFontDic != null && null != defaultFontDic.GetAsDictionary(daFontName)) {
                            daFontDict = defaultFontDic.GetAsDictionary(daFontName);
                        }
                        else {
                            if (normalFontDic != null) {
                                // search normal appearance as a fall back in case it was not found in DR
                                daFontDict = normalFontDic.GetAsDictionary(daFontName);
                            }
                        }
                    }
                }
            }
            if (font != null) {
                fontAndSize[0] = font;
            }
            else {
                if (daFontDict != null) {
                    PdfFont daFont = document != null ? document.GetFont(daFontDict) : PdfFontFactory.CreateFont(daFontDict);
                    fontAndSize[0] = daFont;
                    fontAndSize[2] = daFontName;
                }
                else {
                    fontAndSize[0] = PdfFontFactory.CreateFont();
                }
            }
            if (fontSize >= 0) {
                fontAndSize[1] = fontSize;
            }
            else {
                if (dab[DA_SIZE] != null) {
                    fontAndSize[1] = dab[DA_SIZE];
                }
                else {
                    fontAndSize[1] = (float)DEFAULT_FONT_SIZE;
                }
            }
            if (color == null) {
                color = (Color)dab[DA_COLOR];
            }
            return fontAndSize;
        }

        protected internal static Object[] SplitDAelements(String da) {
            PdfTokenizer tk = new PdfTokenizer(new RandomAccessFileOrArray(new RandomAccessSourceFactory().CreateSource
                (PdfEncodings.ConvertToBytes(da, null))));
            IList<String> stack = new List<String>();
            Object[] ret = new Object[3];
            try {
                while (tk.NextToken()) {
                    if (tk.GetTokenType() == PdfTokenizer.TokenType.Comment) {
                        continue;
                    }
                    if (tk.GetTokenType() == PdfTokenizer.TokenType.Other) {
                        String @operator = tk.GetStringValue();
                        if (@operator.Equals("Tf")) {
                            if (stack.Count >= 2) {
                                ret[DA_FONT] = stack[stack.Count - 2];
                                ret[DA_SIZE] = System.Convert.ToSingle(stack[stack.Count - 1], System.Globalization.CultureInfo.InvariantCulture
                                    );
                            }
                        }
                        else {
                            if (@operator.Equals("g")) {
                                if (stack.Count >= 1) {
                                    float gray = System.Convert.ToSingle(stack[stack.Count - 1], System.Globalization.CultureInfo.InvariantCulture
                                        );
                                    if (gray != 0) {
                                        ret[DA_COLOR] = new DeviceGray(gray);
                                    }
                                }
                            }
                            else {
                                if (@operator.Equals("rg")) {
                                    if (stack.Count >= 3) {
                                        float red = System.Convert.ToSingle(stack[stack.Count - 3], System.Globalization.CultureInfo.InvariantCulture
                                            );
                                        float green = System.Convert.ToSingle(stack[stack.Count - 2], System.Globalization.CultureInfo.InvariantCulture
                                            );
                                        float blue = System.Convert.ToSingle(stack[stack.Count - 1], System.Globalization.CultureInfo.InvariantCulture
                                            );
                                        ret[DA_COLOR] = new DeviceRgb(red, green, blue);
                                    }
                                }
                                else {
                                    if (@operator.Equals("k")) {
                                        if (stack.Count >= 4) {
                                            float cyan = System.Convert.ToSingle(stack[stack.Count - 4], System.Globalization.CultureInfo.InvariantCulture
                                                );
                                            float magenta = System.Convert.ToSingle(stack[stack.Count - 3], System.Globalization.CultureInfo.InvariantCulture
                                                );
                                            float yellow = System.Convert.ToSingle(stack[stack.Count - 2], System.Globalization.CultureInfo.InvariantCulture
                                                );
                                            float black = System.Convert.ToSingle(stack[stack.Count - 1], System.Globalization.CultureInfo.InvariantCulture
                                                );
                                            ret[DA_COLOR] = new DeviceCmyk(cyan, magenta, yellow, black);
                                        }
                                    }
                                }
                            }
                        }
                        stack.Clear();
                    }
                    else {
                        stack.Add(tk.GetStringValue());
                    }
                }
            }
            catch (Exception) {
            }
            return ret;
        }

        /// <summary>Draws the visual appearance of text in a form field.</summary>
        /// <param name="rect">The location on the page for the list field</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">The size of the font</param>
        /// <param name="value">The initial value</param>
        /// <param name="appearance">The appearance</param>
        protected internal virtual void DrawTextAppearance(Rectangle rect, PdfFont font, float fontSize, String value
            , PdfFormXObject appearance) {
            PdfStream stream = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfResources resources = appearance.GetResources();
            PdfCanvas canvas = new PdfCanvas(stream, resources, GetDocument());
            SetDefaultAppearance(GenerateDefaultAppearanceString(font, fontSize, color, resources));
            float height = rect.GetHeight();
            float width = rect.GetWidth();
            PdfFormXObject xObject = new PdfFormXObject(new Rectangle(0, 0, width, height));
            DrawBorder(canvas, xObject, width, height);
            if (IsPassword()) {
                value = ObfuscatePassword(value);
            }
            canvas.BeginVariableText().SaveState().NewPath();
            TextAlignment? textAlignment = ConvertJustificationToTextAlignment();
            float x = X_OFFSET;
            if (textAlignment == TextAlignment.RIGHT) {
                x = rect.GetWidth();
            }
            else {
                if (textAlignment == TextAlignment.CENTER) {
                    x = rect.GetWidth() / 2;
                }
            }
            iText.Layout.Canvas modelCanvas = new iText.Layout.Canvas(canvas, GetDocument(), new Rectangle(0, -height, 
                0, 2 * height));
            modelCanvas.SetProperty(Property.APPEARANCE_STREAM_LAYOUT, true);
            // check if /Comb has been set
            if (this.GetFieldFlag(PdfTextFormField.FF_COMB) && null != this.GetPdfObject().GetAsNumber(PdfName.MaxLen)
                ) {
                // calculate space per character
                PdfNumber maxLenEntry = this.GetPdfObject().GetAsNumber(PdfName.MaxLen);
                int maxLen = maxLenEntry.IntValue();
                float widthPerCharacter = width / maxLen;
                Paragraph paragraph = new Paragraph().SetFont(font).SetFontSize(fontSize).SetMultipliedLeading(1);
                if (color != null) {
                    paragraph.SetFontColor(color);
                }
                int numberOfCharacters = maxLen >= value.Length ? value.Length : maxLen;
                for (int i = 0; i < numberOfCharacters; i++) {
                    // Get width of each character
                    String characterToPlace = value.JSubstring(i, i + 1);
                    float characterWidth = font.GetWidth(characterToPlace, fontSize);
                    // Find x-offset for this character so that we can place it in the center of this comb-section
                    float xOffset = characterWidth == 0 ? characterWidth : (widthPerCharacter - characterWidth) / 2;
                    paragraph.SetPaddings(0f, xOffset, 0f, xOffset);
                    paragraph.Add(characterToPlace);
                    modelCanvas.ShowTextAligned(paragraph, widthPerCharacter * i, 0, textAlignment);
                    paragraph.GetChildren().JRemoveAt(0);
                }
            }
            else {
                if (this.GetFieldFlag(PdfTextFormField.FF_COMB)) {
                    ILog logger = LogManager.GetLogger(typeof(iText.Forms.Fields.PdfFormField));
                    logger.Error(MessageFormatUtil.Format(iText.IO.LogMessageConstant.COMB_FLAG_MAY_BE_SET_ONLY_IF_MAXLEN_IS_PRESENT
                        ));
                }
                Paragraph paragraph = new Paragraph(value).SetFont(font).SetFontSize(fontSize).SetMultipliedLeading(1).SetPaddings
                    (0, X_OFFSET, 0, X_OFFSET);
                if (color != null) {
                    paragraph.SetFontColor(color);
                }
                modelCanvas.ShowTextAligned(paragraph, x, rect.GetHeight() / 2, textAlignment, VerticalAlignment.MIDDLE);
            }
            canvas.RestoreState().EndVariableText();
            appearance.GetPdfObject().SetData(stream.GetBytes());
        }

        /// <summary>Draws the visual appearance of multiline text in a form field.</summary>
        /// <param name="rect">The location on the page for the list field</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">The size of the font</param>
        /// <param name="value">The initial value</param>
        /// <param name="appearance">The appearance</param>
        protected internal virtual void DrawMultiLineTextAppearance(Rectangle rect, PdfFont font, float fontSize, 
            String value, PdfFormXObject appearance) {
            PdfStream stream = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfResources resources = appearance.GetResources();
            PdfCanvas canvas = new PdfCanvas(stream, resources, GetDocument());
            SetDefaultAppearance(GenerateDefaultAppearanceString(font, fontSize, color, resources));
            float width = rect.GetWidth();
            float height = rect.GetHeight();
            float widthBorder = 6.0f;
            float heightBorder = 2.0f;
            IList<String> strings = font.SplitString(value, fontSize, width - widthBorder);
            DrawBorder(canvas, appearance, width, height);
            canvas.BeginVariableText().SaveState().Rectangle(3, 3, width - widthBorder, height - heightBorder).Clip().
                NewPath();
            iText.Layout.Canvas modelCanvas = new iText.Layout.Canvas(canvas, GetDocument(), new Rectangle(3, 0, Math.
                Max(0, width - widthBorder), Math.Max(0, height - heightBorder)));
            modelCanvas.SetProperty(Property.APPEARANCE_STREAM_LAYOUT, true);
            for (int index = 0; index < strings.Count; index++) {
                bool? isFull = modelCanvas.GetRenderer().GetPropertyAsBoolean(Property.FULL);
                if (true.Equals(isFull)) {
                    break;
                }
                Paragraph paragraph = new Paragraph(strings[index]).SetFont(font).SetFontSize(fontSize).SetMargins(0, 0, 0
                    , 0).SetMultipliedLeading(1);
                paragraph.SetProperty(Property.FORCED_PLACEMENT, true);
                paragraph.SetTextAlignment(ConvertJustificationToTextAlignment());
                if (color != null) {
                    paragraph.SetFontColor(color);
                }
                PdfArray indices = GetPdfObject().GetAsArray(PdfName.I);
                if (indices != null && indices.Size() > 0) {
                    foreach (PdfObject ind in indices) {
                        if (!ind.IsNumber()) {
                            continue;
                        }
                        if (((PdfNumber)ind).GetValue() == index) {
                            paragraph.SetBackgroundColor(new DeviceRgb(10, 36, 106));
                            paragraph.SetFontColor(ColorConstants.LIGHT_GRAY);
                        }
                    }
                }
                modelCanvas.Add(paragraph);
            }
            canvas.RestoreState().EndVariableText();
            appearance.GetPdfObject().SetData(stream.GetBytes());
        }

        /// <summary>Draws a border using the borderWidth and borderColor of the form field.</summary>
        /// <param name="canvas">
        /// The
        /// <see cref="iText.Kernel.Pdf.Canvas.PdfCanvas"/>
        /// on which to draw
        /// </param>
        /// <param name="xObject">The PdfFormXObject</param>
        /// <param name="width">The width of the rectangle to draw</param>
        /// <param name="height">The height of the rectangle to draw</param>
        protected internal virtual void DrawBorder(PdfCanvas canvas, PdfFormXObject xObject, float width, float height
            ) {
            canvas.SaveState();
            float borderWidth = GetBorderWidth();
            PdfDictionary bs = GetWidgets()[0].GetBorderStyle();
            if (borderWidth < 0) {
                borderWidth = 0;
            }
            if (backgroundColor != null) {
                canvas.SetFillColor(backgroundColor).Rectangle(0, 0, width, height).Fill();
            }
            if (borderWidth > 0 && borderColor != null) {
                borderWidth = Math.Max(1, borderWidth);
                canvas.SetStrokeColor(borderColor).SetLineWidth(borderWidth);
                if (bs != null) {
                    PdfName borderType = bs.GetAsName(PdfName.S);
                    if (borderType != null && borderType.Equals(PdfName.D)) {
                        PdfArray dashArray = bs.GetAsArray(PdfName.D);
                        int unitsOn = dashArray != null ? (dashArray.Size() > 0 ? (dashArray.GetAsNumber(0) != null ? dashArray.GetAsNumber
                            (0).IntValue() : 3) : 3) : 3;
                        int unitsOff = dashArray != null ? (dashArray.Size() > 1 ? (dashArray.GetAsNumber(1) != null ? dashArray.GetAsNumber
                            (1).IntValue() : unitsOn) : unitsOn) : unitsOn;
                        canvas.SetLineDash(unitsOn, unitsOff, 0);
                    }
                }
                canvas.Rectangle(0, 0, width, height).Stroke();
            }
            ApplyRotation(xObject, height, width);
            canvas.RestoreState();
        }

        protected internal virtual void DrawRadioBorder(PdfCanvas canvas, PdfFormXObject xObject, float width, float
             height) {
            canvas.SaveState();
            float borderWidth = GetBorderWidth();
            float cx = width / 2;
            float cy = height / 2;
            if (borderWidth < 0) {
                borderWidth = 0;
            }
            float r = (Math.Min(width, height) - borderWidth) / 2;
            if (backgroundColor != null) {
                canvas.SetFillColor(backgroundColor).Circle(cx, cy, r + borderWidth / 2).Fill();
            }
            if (borderWidth > 0 && borderColor != null) {
                borderWidth = Math.Max(1, borderWidth);
                canvas.SetStrokeColor(borderColor).SetLineWidth(borderWidth).Circle(cx, cy, r).Stroke();
            }
            ApplyRotation(xObject, height, width);
            canvas.RestoreState();
        }

        /// <summary>Draws the appearance of a radio button with a specified value.</summary>
        /// <param name="width">the width of the radio button to draw</param>
        /// <param name="height">the height of the radio button to draw</param>
        /// <param name="value">the value of the button</param>
        protected internal virtual void DrawRadioAppearance(float width, float height, String value) {
            Rectangle rect = new Rectangle(0, 0, width, height);
            PdfWidgetAnnotation widget = GetWidgets()[0];
            widget.SetNormalAppearance(new PdfDictionary());
            //On state
            PdfStream streamOn = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvasOn = new PdfCanvas(streamOn, new PdfResources(), GetDocument());
            PdfFormXObject xObjectOn = new PdfFormXObject(rect);
            DrawRadioBorder(canvasOn, xObjectOn, width, height);
            DrawRadioField(canvasOn, width, height, true);
            xObjectOn.GetPdfObject().GetOutputStream().WriteBytes(streamOn.GetBytes());
            widget.GetNormalAppearanceObject().Put(new PdfName(value), xObjectOn.GetPdfObject());
            //Off state
            PdfStream streamOff = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvasOff = new PdfCanvas(streamOff, new PdfResources(), GetDocument());
            PdfFormXObject xObjectOff = new PdfFormXObject(rect);
            DrawRadioBorder(canvasOff, xObjectOff, width, height);
            xObjectOff.GetPdfObject().GetOutputStream().WriteBytes(streamOff.GetBytes());
            widget.GetNormalAppearanceObject().Put(new PdfName("Off"), xObjectOff.GetPdfObject());
            if (pdfAConformanceLevel != null && (pdfAConformanceLevel.GetPart().Equals("2") || pdfAConformanceLevel.GetPart
                ().Equals("3"))) {
                xObjectOn.GetResources();
                xObjectOff.GetResources();
            }
        }

        /// <summary>Draws the appearance of a radio button with a specified value.</summary>
        /// <param name="width">the width of the radio button to draw</param>
        /// <param name="height">the height of the radio button to draw</param>
        /// <param name="value">the value of the button</param>
        [System.ObsoleteAttribute(@"Please, use DrawRadioAppearance(float, float, System.String) instead.")]
        protected internal virtual void DrawPdfA1RadioAppearance(float width, float height, String value) {
            PdfStream stream = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvas = new PdfCanvas(stream, new PdfResources(), GetDocument());
            Rectangle rect = new Rectangle(0, 0, width, height);
            PdfFormXObject xObject = new PdfFormXObject(rect);
            DrawBorder(canvas, xObject, width, height);
            DrawRadioField(canvas, rect.GetWidth(), rect.GetHeight(), !"Off".Equals(value));
            PdfDictionary normalAppearance = new PdfDictionary();
            normalAppearance.Put(new PdfName(value), xObject.GetPdfObject());
            PdfWidgetAnnotation widget = GetWidgets()[0];
            xObject.GetPdfObject().GetOutputStream().WriteBytes(stream.GetBytes());
            widget.SetNormalAppearance(normalAppearance);
        }

        /// <summary>Draws a radio button.</summary>
        /// <param name="canvas">
        /// the
        /// <see cref="iText.Kernel.Pdf.Canvas.PdfCanvas"/>
        /// on which to draw
        /// </param>
        /// <param name="width">the width of the radio button to draw</param>
        /// <param name="height">the height of the radio button to draw</param>
        /// <param name="on">required to be <code>true</code> for fulfilling the drawing operation</param>
        protected internal virtual void DrawRadioField(PdfCanvas canvas, float width, float height, bool on) {
            canvas.SaveState();
            if (on) {
                canvas.ResetFillColorRgb();
                DrawingUtil.DrawCircle(canvas, width / 2, height / 2, Math.Min(width, height) / 4);
            }
            canvas.RestoreState();
        }

        /// <summary>Draws the appearance of a checkbox with a specified state value.</summary>
        /// <param name="width">the width of the checkbox to draw</param>
        /// <param name="height">the height of the checkbox to draw</param>
        /// <param name="onStateName">the state of the form field that will be drawn</param>
        protected internal virtual void DrawCheckAppearance(float width, float height, String onStateName) {
            float fontSize = this.fontSize < 0 ? 0 : this.fontSize;
            Rectangle rect = new Rectangle(0, 0, width, height);
            PdfStream streamOn = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvasOn = new PdfCanvas(streamOn, new PdfResources(), GetDocument());
            PdfFormXObject xObjectOn = new PdfFormXObject(rect);
            DrawBorder(canvasOn, xObjectOn, width, height);
            DrawCheckBox(canvasOn, width, height, fontSize, true);
            xObjectOn.GetPdfObject().GetOutputStream().WriteBytes(streamOn.GetBytes());
            xObjectOn.GetResources().AddFont(GetDocument(), GetFont());
            PdfStream streamOff = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvasOff = new PdfCanvas(streamOff, new PdfResources(), GetDocument());
            PdfFormXObject xObjectOff = new PdfFormXObject(rect);
            DrawBorder(canvasOff, xObjectOff, width, height);
            DrawCheckBox(canvasOff, width, height, fontSize, false);
            xObjectOff.GetPdfObject().GetOutputStream().WriteBytes(streamOff.GetBytes());
            xObjectOff.GetResources().AddFont(GetDocument(), GetFont());
            SetDefaultAppearance(GenerateDefaultAppearanceString(font, fontSize, color, xObjectOn.GetResources()));
            PdfDictionary normalAppearance = new PdfDictionary();
            normalAppearance.Put(new PdfName(onStateName), xObjectOn.GetPdfObject());
            normalAppearance.Put(new PdfName("Off"), xObjectOff.GetPdfObject());
            PdfDictionary mk = new PdfDictionary();
            mk.Put(PdfName.CA, new PdfString(text));
            PdfWidgetAnnotation widget = GetWidgets()[0];
            widget.GetPdfObject().Put(PdfName.MK, mk);
            widget.SetNormalAppearance(normalAppearance);
        }

        //Actually it's just PdfA check appearance. According to corrigendum there is no difference between them
        protected internal virtual void DrawPdfA2CheckAppearance(float width, float height, String onStateName, int
             checkType) {
            this.checkType = checkType;
            Rectangle rect = new Rectangle(0, 0, width, height);
            PdfStream streamOn = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvasOn = new PdfCanvas(streamOn, new PdfResources(), GetDocument());
            PdfFormXObject xObjectOn = new PdfFormXObject(rect);
            xObjectOn.GetResources();
            DrawBorder(canvasOn, xObjectOn, width, height);
            DrawPdfACheckBox(canvasOn, width, height, true);
            xObjectOn.GetPdfObject().GetOutputStream().WriteBytes(streamOn.GetBytes());
            PdfStream streamOff = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvasOff = new PdfCanvas(streamOff, new PdfResources(), GetDocument());
            PdfFormXObject xObjectOff = new PdfFormXObject(rect);
            xObjectOff.GetResources();
            DrawBorder(canvasOff, xObjectOff, width, height);
            xObjectOff.GetPdfObject().GetOutputStream().WriteBytes(streamOff.GetBytes());
            PdfDictionary normalAppearance = new PdfDictionary();
            normalAppearance.Put(new PdfName(onStateName), xObjectOn.GetPdfObject());
            normalAppearance.Put(new PdfName("Off"), xObjectOff.GetPdfObject());
            PdfDictionary mk = new PdfDictionary();
            mk.Put(PdfName.CA, new PdfString(text));
            PdfWidgetAnnotation widget = GetWidgets()[0];
            widget.Put(PdfName.MK, mk);
            widget.SetNormalAppearance(normalAppearance);
        }

        [System.ObsoleteAttribute(@"use DrawPdfA2CheckAppearance(float, float, System.String, int) instead.")]
        protected internal virtual void DrawPdfA1CheckAppearance(float width, float height, String selectedValue, 
            int checkType) {
            PdfStream stream = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            PdfCanvas canvas = new PdfCanvas(stream, new PdfResources(), GetDocument());
            Rectangle rect = new Rectangle(0, 0, width, height);
            PdfFormXObject xObject = new PdfFormXObject(rect);
            this.checkType = checkType;
            DrawBorder(canvas, xObject, width, height);
            DrawPdfACheckBox(canvas, width, height, !"Off".Equals(selectedValue));
            xObject.GetPdfObject().GetOutputStream().WriteBytes(stream.GetBytes());
            PdfDictionary normalAppearance = new PdfDictionary();
            normalAppearance.Put(new PdfName(selectedValue), xObject.GetPdfObject());
            PdfDictionary mk = new PdfDictionary();
            mk.Put(PdfName.CA, new PdfString(text));
            PdfWidgetAnnotation widget = GetWidgets()[0];
            widget.Put(PdfName.MK, mk);
            widget.SetNormalAppearance(normalAppearance);
        }

        /// <summary>Draws the appearance for a push button.</summary>
        /// <param name="width">the width of the pushbutton</param>
        /// <param name="height">the width of the pushbutton</param>
        /// <param name="text">the text to display on the button</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        /// <returns>
        /// a new
        /// <see cref="iText.Kernel.Pdf.Xobject.PdfFormXObject"/>
        /// </returns>
        /// <seealso cref="DrawPushButtonAppearance(float, float, System.String, iText.Kernel.Font.PdfFont, iText.Kernel.Pdf.PdfName, float)
        ///     "/>
        [System.ObsoleteAttribute(@"Will be removed in 7.2.")]
        protected internal virtual PdfFormXObject DrawPushButtonAppearance(float width, float height, String text, 
            PdfFont font, float fontSize) {
            return DrawPushButtonAppearance(width, height, text, font, null, fontSize);
        }

        /// <summary>Draws the appearance for a push button.</summary>
        /// <param name="width">the width of the pushbutton</param>
        /// <param name="height">the width of the pushbutton</param>
        /// <param name="text">the text to display on the button</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontName">fontName in DR.</param>
        /// <param name="fontSize">the size of the font</param>
        /// <returns>
        /// a new
        /// <see cref="iText.Kernel.Pdf.Xobject.PdfFormXObject"/>
        /// </returns>
        protected internal virtual PdfFormXObject DrawPushButtonAppearance(float width, float height, String text, 
            PdfFont font, PdfName fontName, float fontSize) {
            PdfStream stream = (PdfStream)new PdfStream().MakeIndirect(GetDocument());
            AppearanceResources resources = new AppearanceResources().AddFontFromDefaultResources(fontName, font);
            PdfCanvas canvas = new PdfCanvas(stream, resources, GetDocument());
            AppearanceXObject xObject = new AppearanceXObject(new Rectangle(0, 0, width, height));
            DrawBorder(canvas, xObject, width, height);
            if (img != null) {
                PdfImageXObject imgXObj = new PdfImageXObject(img);
                canvas.AddXObject(imgXObj, width - borderWidth, 0, 0, height - borderWidth, borderWidth / 2, borderWidth /
                     2);
                xObject.GetResources().AddImage(imgXObj);
            }
            else {
                if (form != null) {
                    canvas.AddXObject(form, (height - borderWidth) / form.GetHeight(), 0, 0, (height - borderWidth) / form.GetHeight
                        (), borderWidth / 2, borderWidth / 2);
                    xObject.GetResources().AddForm(form);
                }
                else {
                    DrawButton(canvas, 0, 0, width, height, text, font, fontSize);
                    xObject.AddFontFromDR(fontName, font);
                    SetDefaultAppearance(GenerateDefaultAppearanceString(font, fontSize, color, resources));
                    xObject.GetResources().AddFont(GetDocument(), font);
                }
            }
            xObject.GetPdfObject().GetOutputStream().WriteBytes(stream.GetBytes());
            return xObject;
        }

        /// <summary>Performs the low-level drawing operations to draw a button object.</summary>
        /// <param name="canvas">
        /// the
        /// <see cref="iText.Kernel.Pdf.Canvas.PdfCanvas"/>
        /// of the page to draw on.
        /// </param>
        /// <param name="x">the x coordinate of the lower left corner of the button rectangle</param>
        /// <param name="y">the y coordinate of the lower left corner of the button rectangle</param>
        /// <param name="width">the width of the button</param>
        /// <param name="height">the width of the button</param>
        /// <param name="text">the text to display on the button</param>
        /// <param name="font">
        /// a
        /// <see cref="iText.Kernel.Font.PdfFont"/>
        /// </param>
        /// <param name="fontSize">the size of the font</param>
        protected internal virtual void DrawButton(PdfCanvas canvas, float x, float y, float width, float height, 
            String text, PdfFont font, float fontSize) {
            if (color == null) {
                color = ColorConstants.BLACK;
            }
            if (text == null) {
                text = "";
            }
            Paragraph paragraph = new Paragraph(text).SetFont(font).SetFontSize(fontSize).SetMargin(0).SetMultipliedLeading
                (1).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            iText.Layout.Canvas modelCanvas = new iText.Layout.Canvas(canvas, GetDocument(), new Rectangle(0, -height, 
                width, 2 * height));
            modelCanvas.SetProperty(Property.APPEARANCE_STREAM_LAYOUT, true);
            modelCanvas.ShowTextAligned(paragraph, width / 2, height / 2, TextAlignment.CENTER, VerticalAlignment.MIDDLE
                );
        }

        /// <summary>Performs the low-level drawing operations to draw a checkbox object.</summary>
        /// <param name="canvas">
        /// the
        /// <see cref="iText.Kernel.Pdf.Canvas.PdfCanvas"/>
        /// of the page to draw on.
        /// </param>
        /// <param name="width">the width of the button</param>
        /// <param name="height">the width of the button</param>
        /// <param name="fontSize">the size of the font</param>
        /// <param name="on">the boolean value of the checkbox</param>
        protected internal virtual void DrawCheckBox(PdfCanvas canvas, float width, float height, float fontSize, 
            bool on) {
            if (!on) {
                return;
            }
            if (checkType == TYPE_CROSS) {
                DrawingUtil.DrawCross(canvas, width, height, borderWidth);
                return;
            }
            PdfFont ufont = GetFont();
            if (fontSize <= 0) {
                fontSize = ApproximateFontSizeToFitBBox(ufont, new Rectangle(width, height), text);
            }
            // PdfFont gets all width in 1000 normalized units
            canvas.BeginText().SetFontAndSize(ufont, fontSize).ResetFillColorRgb().SetTextMatrix((width - ufont.GetWidth
                (text, fontSize)) / 2, (height - ufont.GetAscent(text, fontSize)) / 2).ShowText(text).EndText();
        }

        protected internal virtual void DrawPdfACheckBox(PdfCanvas canvas, float width, float height, bool on) {
            if (!on) {
                return;
            }
            switch (checkType) {
                case TYPE_CHECK: {
                    DrawingUtil.DrawPdfACheck(canvas, width, height);
                    break;
                }

                case TYPE_CIRCLE: {
                    DrawingUtil.DrawPdfACircle(canvas, width, height);
                    break;
                }

                case TYPE_CROSS: {
                    DrawingUtil.DrawPdfACross(canvas, width, height);
                    break;
                }

                case TYPE_DIAMOND: {
                    DrawingUtil.DrawPdfADiamond(canvas, width, height);
                    break;
                }

                case TYPE_SQUARE: {
                    DrawingUtil.DrawPdfASquare(canvas, width, height);
                    break;
                }

                case TYPE_STAR: {
                    DrawingUtil.DrawPdfAStar(canvas, width, height);
                    break;
                }
            }
        }

        private PdfObject GetAcroFormKey(PdfName key, int type) {
            PdfObject acroFormKey = null;
            PdfDocument document = GetDocument();
            if (document != null) {
                PdfDictionary acroFormDictionary = document.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.AcroForm);
                if (acroFormDictionary != null) {
                    acroFormKey = acroFormDictionary.Get(key);
                }
            }
            return (acroFormKey != null && acroFormKey.GetObjectType() == type) ? acroFormKey : null;
        }

        private TextAlignment? ConvertJustificationToTextAlignment() {
            int? justification = GetJustification();
            if (justification == null) {
                justification = 0;
            }
            TextAlignment? textAlignment = TextAlignment.LEFT;
            if (justification == ALIGN_RIGHT) {
                textAlignment = TextAlignment.RIGHT;
            }
            else {
                if (justification == ALIGN_CENTER) {
                    textAlignment = TextAlignment.CENTER;
                }
            }
            return textAlignment;
        }

        private PdfName GetTypeFromParent(PdfDictionary field) {
            PdfDictionary parent = field.GetAsDictionary(PdfName.Parent);
            PdfName formType = field.GetAsName(PdfName.FT);
            if (parent != null) {
                formType = parent.GetAsName(PdfName.FT);
                if (formType == null) {
                    formType = GetTypeFromParent(parent);
                }
            }
            return formType;
        }

        private String ObfuscatePassword(String text) {
            char[] pchar = new char[text.Length];
            for (int i = 0; i < text.Length; i++) {
                pchar[i] = '*';
            }
            return new String(pchar);
        }

        private void ApplyRotation(PdfFormXObject xObject, float height, float width) {
            switch (rotation) {
                case 90: {
                    xObject.GetPdfObject().Put(PdfName.Matrix, new PdfArray(new float[] { 0, 1, -1, 0, height, 0 }));
                    break;
                }

                case 180: {
                    xObject.GetPdfObject().Put(PdfName.Matrix, new PdfArray(new float[] { -1, 0, 0, -1, width, height }));
                    break;
                }

                case 270: {
                    xObject.GetPdfObject().Put(PdfName.Matrix, new PdfArray(new float[] { 0, -1, 1, 0, 0, width }));
                    break;
                }
            }
        }

        private static String OptionsArrayToString(PdfArray options) {
            StringBuilder stringBuffer = new StringBuilder();
            foreach (PdfObject obj in options) {
                if (obj.IsString()) {
                    stringBuffer.Append(((PdfString)obj).ToUnicodeString()).Append('\n');
                }
                else {
                    if (obj.IsArray()) {
                        PdfObject element = ((PdfArray)obj).Get(1);
                        if (element.IsString()) {
                            stringBuffer.Append(((PdfString)element).ToUnicodeString()).Append('\n');
                        }
                    }
                }
            }
            stringBuffer.DeleteCharAt(stringBuffer.Length - 1);
            // last '\n'
            return stringBuffer.ToString();
        }

        private static double DegreeToRadians(double angle) {
            return Math.PI * angle / 180.0;
        }

        private PdfObject GetValueFromAppearance(PdfObject appearanceDict, PdfName key) {
            if (appearanceDict is PdfDictionary) {
                return ((PdfDictionary)appearanceDict).Get(key);
            }
            return null;
        }

        private void RetrieveStyles() {
            // For now we retrieve styles only in case of merged widget with the field,
            // for one field might contain several widgets with their own different styles
            // and it's unclear how to handle it with the way iText processes fields with multiple widgets currently.
            PdfName subType = GetPdfObject().GetAsName(PdfName.Subtype);
            if (subType != null && subType.Equals(PdfName.Widget)) {
                PdfDictionary appearanceCharacteristics = GetPdfObject().GetAsDictionary(PdfName.MK);
                if (appearanceCharacteristics != null) {
                    backgroundColor = GetColor(appearanceCharacteristics, PdfName.BG);
                    Color extractedBorderColor = GetColor(appearanceCharacteristics, PdfName.BC);
                    if (extractedBorderColor != null) {
                        borderColor = extractedBorderColor;
                    }
                }
            }
        }

        private Color GetColor(PdfDictionary appearanceCharacteristics, PdfName property) {
            PdfArray colorData = appearanceCharacteristics.GetAsArray(property);
            if (colorData != null) {
                float[] backgroundFloat = new float[colorData.Size()];
                for (int i = 0; i < colorData.Size(); i++) {
                    backgroundFloat[i] = colorData.GetAsNumber(i).FloatValue();
                }
                switch (colorData.Size()) {
                    case 0: {
                        return null;
                    }

                    case 1: {
                        return new DeviceGray(backgroundFloat[0]);
                    }

                    case 3: {
                        return new DeviceRgb(backgroundFloat[0], backgroundFloat[1], backgroundFloat[2]);
                    }

                    case 4: {
                        return new DeviceCmyk(backgroundFloat[0], backgroundFloat[1], backgroundFloat[2], backgroundFloat[3]);
                    }
                }
            }
            return null;
        }
    }
}
