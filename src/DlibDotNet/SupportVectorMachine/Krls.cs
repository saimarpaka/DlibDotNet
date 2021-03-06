﻿using System;
using System.IO;
using DlibDotNet.Extensions;

// ReSharper disable once CheckNamespace
namespace DlibDotNet
{

    public sealed class Krls<TScalar, TKernel> : DlibObject
        where TScalar : struct
        where TKernel : KernelBase
    {

        #region Fields

        private readonly KernelBaseParameter _Parameter;

        private readonly Bridge<TScalar> _Bridge;

        #endregion

        #region Constructors

        public Krls(TKernel kernelBase,
                    double tolerance = 0.001d,
                    uint maxDictionarySize = 1000000)
        {
            if (kernelBase == null)
                throw new ArgumentNullException(nameof(kernelBase));

            kernelBase.ThrowIfDisposed();

            this._Bridge = CreateBridge(new KernelBaseParameter(kernelBase));

            this._Parameter = new KernelBaseParameter(kernelBase);
            var error = NativeMethods.krls_new(this._Parameter.KernelType.ToNativeKernelType(),
                                               this._Parameter.SampleType.ToNativeMatrixElementType(),
                                               this._Parameter.TemplateRows,
                                               this._Parameter.TemplateColumns,
                                               kernelBase.NativePtr,
                                               tolerance,
                                               maxDictionarySize,
                                               out var ret);
            switch (error)
            {
                case NativeMethods.ErrorType.MatrixElementTypeNotSupport:
                    throw new ArgumentException($"{this._Parameter.SampleType} is not supported.");
                case NativeMethods.ErrorType.MatrixElementTemplateSizeNotSupport:
                    throw new ArgumentException($"{nameof(this._Parameter.TemplateColumns)} or {nameof(this._Parameter.TemplateRows)} is not supported.");
                case NativeMethods.ErrorType.SvmKernelNotSupport:
                    throw new ArgumentException($"{this._Parameter.KernelType} is not supported.");
            }

            this.NativePtr = ret;
        }

        internal Krls(IntPtr ptr,
                      KernelBaseParameter parameter,
                      bool isEnabledDispose = true) :
            base(isEnabledDispose)
        {
            this._Bridge = CreateBridge(parameter);

            this._Parameter = parameter;
            this.NativePtr = ptr;
        }

        #endregion

        #region Properties

        public uint DictionarySize
        {
            get
            {
                this.ThrowIfDisposed();
                NativeMethods.krls_dictionary_size(this._Parameter.KernelType.ToNativeKernelType(),
                                                   this._Parameter.SampleType.ToNativeMatrixElementType(),
                                                   this._Parameter.TemplateRows,
                                                   this._Parameter.TemplateColumns,
                                                   this.NativePtr,
                                                   out var ret);

                return ret;
            }
        }

        internal KernelBaseParameter Parameter => this._Parameter;

        public TKernel Kernel
        {
            get
            {
                this.ThrowIfDisposed();

                var error = NativeMethods.krls_get_kernel(this._Parameter.KernelType.ToNativeKernelType(),
                                                          this._Parameter.SampleType.ToNativeMatrixElementType(),
                                                          this._Parameter.TemplateRows,
                                                          this._Parameter.TemplateColumns,
                                                          this.NativePtr,
                                                          out var ret);

                return KernelFactory.Create<TKernel, TScalar, Matrix<TScalar>>(ret,
                                                                               this._Parameter.KernelType,
                                                                               this._Parameter.TemplateRows,
                                                                               this._Parameter.TemplateColumns,
                                                                               false);
            }
        }

        #endregion

        #region Methods

        public static void Deserialize(string path, ref Krls<TScalar, TKernel> krls)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is not found", path);

            var str = Dlib.Encoding.GetBytes(path);
            var error = NativeMethods.deserialize_krls(str,
                                                       str.Length,
                                                       krls.Parameter.KernelType.ToNativeKernelType(),
                                                       krls.Parameter.SampleType.ToNativeMatrixElementType(),
                                                       krls.Parameter.TemplateRows,
                                                       krls.Parameter.TemplateColumns,
                                                       krls.NativePtr,
                                                       out var errorMessage);

            switch (error)
            {
                case NativeMethods.ErrorType.GeneralSerialization:
                    throw new SerializationException(StringHelper.FromStdString(errorMessage, true));
                case NativeMethods.ErrorType.MatrixElementTypeNotSupport:
                    throw new ArgumentException($"{krls.Parameter.SampleType} is not supported.");
                case NativeMethods.ErrorType.MatrixElementTemplateSizeNotSupport:
                    throw new ArgumentException($"{nameof(krls.Parameter.TemplateColumns)} or {nameof(krls.Parameter.TemplateRows)} is not supported.");
                case NativeMethods.ErrorType.SvmKernelNotSupport:
                    throw new ArgumentException($"{krls.Parameter.KernelType} is not supported.");
            }
        }

        public DecisionFunction<TScalar, TKernel> GetDecisionFunction()
        {
            this.ThrowIfDisposed();
            NativeMethods.krls_get_decision_function(this._Parameter.KernelType.ToNativeKernelType(),
                                                     this._Parameter.SampleType.ToNativeMatrixElementType(),
                                                     this._Parameter.TemplateRows,
                                                     this._Parameter.TemplateColumns,
                                                     this.NativePtr,
                                                     out var ret);

            return new DecisionFunction<TScalar, TKernel>(ret, this.Parameter);
        }

        public TScalar Operator(Matrix<TScalar> sample)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));

            this.ThrowIfDisposed();
            sample.ThrowIfDisposed();

            return this._Bridge.Operator(this.NativePtr, sample);
        }

        public static void Serialize(Krls<TScalar, TKernel> krls, string path)
        {
            if (krls == null)
                throw new ArgumentNullException(nameof(krls));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException();

            krls.ThrowIfDisposed();

            var str = Dlib.Encoding.GetBytes(path);
            var ret = NativeMethods.serialize_krls(krls._Parameter.KernelType.ToNativeKernelType(),
                                                   krls._Parameter.SampleType.ToNativeMatrixElementType(),
                                                   krls._Parameter.TemplateRows,
                                                   krls._Parameter.TemplateColumns,
                                                   krls.NativePtr,
                                                   str,
                                                   str.Length,
                                                   out var errorMessage);

            switch (ret)
            {
                case NativeMethods.ErrorType.GeneralSerialization:
                    throw new SerializationException(StringHelper.FromStdString(errorMessage, true));
                case NativeMethods.ErrorType.MatrixElementTypeNotSupport:
                    throw new ArgumentException($"{krls.Parameter.SampleType} is not supported.");
                case NativeMethods.ErrorType.MatrixElementTemplateSizeNotSupport:
                    throw new ArgumentException($"{nameof(krls.Parameter.TemplateColumns)} or {nameof(krls.Parameter.TemplateRows)} is not supported.");
                case NativeMethods.ErrorType.SvmKernelNotSupport:
                    throw new ArgumentException($"{krls.Parameter.KernelType} is not supported.");
            }
        }

        public void Train(Matrix<TScalar> x, TScalar y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));

            this.ThrowIfDisposed();
            x.ThrowIfDisposed();

            this._Bridge.Train(this.NativePtr, x, y);
        }

        #region Overrides 

        /// <summary>
        /// Releases all unmanaged resources.
        /// </summary>
        protected override void DisposeUnmanaged()
        {
            base.DisposeUnmanaged();

            if (this.NativePtr == IntPtr.Zero)
                return;

            NativeMethods.krls_delete(this._Parameter.KernelType.ToNativeKernelType(),
                                      this._Parameter.SampleType.ToNativeMatrixElementType(),
                                      this._Parameter.TemplateRows,
                                      this._Parameter.TemplateColumns,
                                      this.NativePtr);
        }

        #endregion

        #region Helpers

        private static Bridge<TScalar> CreateBridge(KernelBaseParameter parameter)
        {
            switch (parameter.SampleType)
            {
                case MatrixElementTypes.Float:
                    return new FloatBridge(parameter) as Bridge<TScalar>;
                case MatrixElementTypes.Double:
                    return new DoubleBridge(parameter) as Bridge<TScalar>;
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion

        #endregion

        private abstract class Bridge<T>
            where T : struct
        {

            #region Constructors

            protected Bridge(KernelBaseParameter parameter)
            {
                this.Parameter = parameter;
            }

            #endregion

            #region Properties

            protected KernelBaseParameter Parameter
            {
                get;
            }

            #endregion

            #region Methods

            public abstract T Operator(IntPtr obj, Matrix<T> sample);

            public abstract void Train(IntPtr obj, Matrix<T> x, T y);

            #endregion

        }

        private sealed class FloatBridge : Bridge<float>
        {

            #region Constructors

            public FloatBridge(KernelBaseParameter parameter) :
                base(parameter)
            {
            }

            #endregion

            #region Methods

            public override float Operator(IntPtr obj, Matrix<float> sample)
            {
                var err = NativeMethods.krls_operator_float(this.Parameter.KernelType.ToNativeKernelType(),
                                                            this.Parameter.SampleType.ToNativeMatrixElementType(),
                                                            this.Parameter.TemplateRows,
                                                            this.Parameter.TemplateColumns,
                                                            obj,
                                                            sample.NativePtr,
                                                            out var ret);
                return ret;
            }

            public override void Train(IntPtr obj, Matrix<float> x, float y)
            {
                var err = NativeMethods.krls_train_float(this.Parameter.KernelType.ToNativeKernelType(),
                                                         this.Parameter.SampleType.ToNativeMatrixElementType(),
                                                         this.Parameter.TemplateRows,
                                                         this.Parameter.TemplateColumns,
                                                         obj,
                                                         x.NativePtr,
                                                         y);
            }

            #endregion

        }

        private sealed class DoubleBridge : Bridge<double>
        {

            #region Constructors

            public DoubleBridge(KernelBaseParameter parameter) :
                base(parameter)
            {
            }

            #endregion

            #region Methods

            public override double Operator(IntPtr obj, Matrix<double> sample)
            {
                var err = NativeMethods.krls_operator_double(this.Parameter.KernelType.ToNativeKernelType(),
                                                             this.Parameter.SampleType.ToNativeMatrixElementType(),
                                                             this.Parameter.TemplateRows,
                                                             this.Parameter.TemplateColumns,
                                                             obj,
                                                             sample.NativePtr,
                                                             out var ret);
                return ret;
            }

            public override void Train(IntPtr obj, Matrix<double> x, double y)
            {
                var err = NativeMethods.krls_train_double(this.Parameter.KernelType.ToNativeKernelType(),
                                                          this.Parameter.SampleType.ToNativeMatrixElementType(),
                                                          this.Parameter.TemplateRows,
                                                          this.Parameter.TemplateColumns,
                                                          obj,
                                                          x.NativePtr,
                                                          y);
            }

            #endregion

        }

    }

}
